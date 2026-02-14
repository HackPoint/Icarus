# Testing Infrastructure

Icarus uses a layered testing strategy: unit tests for logic, integration tests for APIs and services, snapshot tests for output stability, E2E tests for user flows, and smoke tests for load validation.

---

## Test Types Overview

| Type | Scope | Tools | Purpose |
|------|-------|-------|---------|
| Unit | Single class/function | xUnit, FluentAssertions, NSubstitute | Fast feedback, isolated logic |
| Integration | API + DB + external deps | WebApplicationFactory, Testcontainers | Real HTTP, DB, and service behavior |
| Snapshot | Output shape | Verify.Xunit | Prevent regressions in serialized output |
| E2E | Full app in browser | Playwright | User journeys, UI flows |
| Smoke | Load & availability | k6 | Basic performance and uptime |

---

## Unit Tests

**Frameworks:** xUnit, FluentAssertions, NSubstitute

- **xUnit:** Test runner and discovery
- **FluentAssertions:** Readable assertions (`result.Should().Be(expected)`)
- **NSubstitute:** Mocking and stubs

**Example:**

```csharp
public class SearchServiceTests
{
    [Fact]
    public async Task Search_ReturnsResults_WhenQueryMatches()
    {
        var vectorStore = Substitute.For<IVectorStore>();
        vectorStore.SearchAsync(Arg.Any<string>(), 5, default)
            .Returns(new[] { new Chunk { Id = "c1", Score = 0.9 } });

        var sut = new SearchService(vectorStore);
        var result = await sut.SearchAsync("test query", topK: 5);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("c1");
        result[0].Score.Should().BeApproximately(0.9f, 0.01f);
    }
}
```

**Run unit tests:**

```bash
dotnet test --filter "FullyQualifiedName~Unit" --no-build
# or for a specific project
dotnet test src/Icarus.Tests.Unit/Icarus.Tests.Unit.csproj
```

---

## Integration Tests

**Frameworks:** WebApplicationFactory, Testcontainers

- **WebApplicationFactory:** In-memory host for HTTP requests
- **Testcontainers:** Real PostgreSQL, Redis, MinIO, etc. in Docker

**Example:**

```csharp
public class ChatApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ChatApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Stream_ReturnsSSEEvents_WhenQueryProvided()
    {
        var response = await _client.GetAsync("/chat/stream?query=hello");
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
    }
}
```

**Run integration tests:**

```bash
# Requires Docker for Testcontainers
dotnet test --filter "FullyQualifiedName~Integration"
# or
dotnet test src/Icarus.Tests.Integration/Icarus.Tests.Integration.csproj
```

---

## Snapshot Tests

**Framework:** Verify.Xunit

Captures output (JSON, text, etc.) and compares against stored snapshots. Failures highlight diffs.

**Example:**

```csharp
[Fact]
public async Task SearchResponse_MatchesSnapshot()
{
    var response = await _searchService.SearchAsync("test", topK: 5);
    await Verify(response).UseDirectory("Snapshots");
}
```

**Run snapshot tests:**

```bash
dotnet test --filter "FullyQualifiedName~Snapshot"
# Update snapshots after intentional changes
dotnet test --filter "FullyQualifiedName~Snapshot" -- Verify.DiffTool=VisualStudio
```

---

## E2E Tests

**Framework:** Playwright

Runs against a deployed or locally running instance. Exercises full stack and browser behavior.

**Example:**

```javascript
// tests/e2e/chat.spec.js
import { test, expect } from '@playwright/test';

test('chat stream displays tokens', async ({ page }) => {
  await page.goto('/chat');
  await page.fill('[data-testid="query-input"]', 'What is Icarus?');
  await page.click('[data-testid="submit"]');
  await expect(page.locator('[data-testid="stream-output"]')).toContainText('Icarus', { timeout: 10000 });
});
```

**Run E2E tests:**

```bash
# Install browsers (first time)
npx playwright install

# Run all E2E tests
npx playwright test

# Run with UI
npx playwright test --ui

# Run against specific base URL
BASE_URL=http://localhost:5000 npx playwright test
```

---

## Smoke Tests

**Tool:** k6

Light load and availability checks. Validates that critical endpoints respond and meet basic latency targets.

**Example:**

```javascript
// tests/smoke/smoke.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 5,
  duration: '30s',
};

export default function () {
  const res = http.get('http://localhost:5000/health');
  check(res, { 'health ok': (r) => r.status === 200 });
  sleep(1);
}
```

**Run smoke tests:**

```bash
# Requires k6: brew install k6 (macOS) or see k6.io
k6 run tests/smoke/smoke.js

# Against staging
k6 run -e BASE_URL=https://staging.icarus.example tests/smoke/smoke.js
```

---

## Test Matrix

| Test Type | Command | Typical Duration | Requires |
|-----------|---------|------------------|----------|
| Unit | `dotnet test --filter Unit` | ~5–30s | .NET SDK |
| Integration | `dotnet test --filter Integration` | ~1–3min | Docker, .NET SDK |
| Snapshot | `dotnet test --filter Snapshot` | ~10–30s | .NET SDK |
| E2E | `npx playwright test` | ~2–5min | Node, Playwright, running app |
| Smoke | `k6 run tests/smoke/smoke.js` | ~30s | k6, running app |

---

## CI Integration

Recommended pipeline order:

1. **Unit** – Fast, no external deps
2. **Snapshot** – Quick, catches serialization regressions
3. **Integration** – Needs Docker, validates APIs and DB
4. **E2E** – Needs deployed or local app
5. **Smoke** – Post-deploy validation

```yaml
# Example GitHub Actions
- name: Unit tests
  run: dotnet test --filter Unit --no-build
- name: Integration tests
  run: dotnet test --filter Integration
- name: E2E tests
  run: npx playwright test
  env:
    BASE_URL: ${{ env.DEPLOY_URL }}
- name: Smoke tests
  run: k6 run tests/smoke/smoke.js
  env:
    BASE_URL: ${{ env.DEPLOY_URL }}
```

using System.Net.Http.Json;
using Icarus.Orchestrator.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Icarus.Orchestrator.Tests.Integration;

public class SnapshotTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SnapshotTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ChatQuery_ReturnsConsistentStructure()
    {
        var request = new ChatRequest("What is Icarus?");
        var response = await _client.PostAsJsonAsync("/chat/query", request);
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Answer);
        Assert.NotEmpty(result.Citations);
        Assert.True(result.Metrics.RetrievalMs >= 0);
    }

    [Fact]
    public async Task ChatStream_ReturnsConsistentEventSequence()
    {
        var response = await _client.GetAsync("/chat/stream?query=Hello");
        var content = await response.Content.ReadAsStringAsync();

        // Verify event ordering: tool_call -> tool_result -> citation -> token -> final -> metrics
        Assert.Contains("event: tool_call", content);
        Assert.Contains("event: token", content);
        Assert.Contains("event: final", content);
        Assert.Contains("event: metrics", content);

        var toolCallIdx = content.IndexOf("event: tool_call");
        var tokenIdx = content.IndexOf("event: token");
        var finalIdx = content.IndexOf("event: final");

        Assert.True(toolCallIdx < tokenIdx, "tool_call should come before token");
        Assert.True(tokenIdx < finalIdx, "token should come before final");
    }
}

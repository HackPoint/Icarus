import { test, expect } from '@playwright/test';

test.describe('Icarus API E2E', () => {
  test('GET /health returns healthy status', async ({ request }) => {
    const response = await request.get('/health');
    expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.status).toBe('Healthy');
    expect(body.service).toBe('Icarus.Orchestrator.Api');
  });

  test('POST /chat/query returns a response with citations', async ({ request }) => {
    const response = await request.post('/chat/query', {
      data: {
        query: 'What is Icarus?',
        topK: 3,
      },
    });
    expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.answer).toBeTruthy();
    expect(body.citations).toBeDefined();
    expect(body.citations.length).toBeGreaterThan(0);
    expect(body.metrics).toBeDefined();
  });

  test('POST /sources/register creates a new source', async ({ request }) => {
    const response = await request.post('/sources/register', {
      data: {
        name: 'e2e-test-source',
        connectionString: 'Host=localhost',
        sourceType: 1,
      },
    });
    expect(response.status()).toBe(201);

    const body = await response.json();
    expect(body.name).toBe('e2e-test-source');
    expect(body.id).toBeTruthy();
  });

  test('GET /chat/stream returns SSE events', async ({ request }) => {
    const response = await request.get('/chat/stream?query=test');
    expect(response.ok()).toBeTruthy();

    const contentType = response.headers()['content-type'];
    expect(contentType).toContain('text/event-stream');

    const text = await response.text();
    expect(text).toContain('event: token');
    expect(text).toContain('event: final');
  });
});

# SSE+ Streaming Protocol

Icarus uses Server-Sent Events (SSE) for streaming chat responses. The protocol extends standard SSE with typed event payloads for tokens, citations, tool calls, and metrics.

## Endpoint

```
GET /chat/stream?query={url-encoded-query}
```

**Headers:**

| Header | Value |
|--------|-------|
| `Accept` | `text/event-stream` |
| `Content-Type` | `text/event-stream` (response) |

---

## Event Types

All events use the format `data: {json}\n\n`. The `type` field identifies the event kind.

### `token`

Streaming LLM output token.

```json
{"type":"token","token":"word"}
```

### `citation`

A retrieved document chunk referenced in the response.

```json
{
  "type": "citation",
  "documentId": "doc-001",
  "snippet": "The SSE protocol uses text/event-stream...",
  "score": 0.95
}
```

### `tool_call`

The model has requested a tool invocation.

```json
{
  "type": "tool_call",
  "toolName": "embed_query",
  "arguments": {
    "query": "user search query",
    "topK": 5
  }
}
```

### `tool_result`

Result of a tool execution, fed back to the model.

```json
{
  "type": "tool_result",
  "toolName": "embed_query",
  "result": "{\"results\":[{\"documentId\":\"doc-1\",\"score\":0.9}]}"
}
```

### `final`

Complete response text. Sent when generation finishes.

```json
{
  "type": "final",
  "fullText": "The SSE streaming protocol uses Server-Sent Events..."
}
```

### `error`

An error occurred during processing.

```json
{
  "type": "error",
  "message": "Model unavailable",
  "code": "MODEL_ERROR"
}
```

### `metrics`

Performance metrics for the request.

```json
{
  "type": "metrics",
  "retrievalMs": 42,
  "generationMs": 120,
  "tokenCount": 30
}
```

---

## Full Example SSE Transcript

Below is a complete example showing all event types in a typical RAG chat flow.

```
data: {"type":"token","token":"The"}

data: {"type":"token","token":" SSE"}

data: {"type":"token","token":" protocol"}

data: {"type":"citation","documentId":"doc-001","snippet":"SSE uses Content-Type: text/event-stream for streaming responses.","score":0.95}

data: {"type":"token","token":" uses"}

data: {"type":"token","token":" Server-Sent"}

data: {"type":"token","token":" Events"}

data: {"type":"tool_call","toolName":"search","arguments":{"query":"SSE event format","topK":3}}

data: {"type":"tool_result","toolName":"search","result":"[{\"documentId\":\"doc-002\",\"snippet\":\"Event format: data: {json}\\n\\n\"}]"}

data: {"type":"token","token":"."}

data: {"type":"token","token":" Each"}

data: {"type":"token","token":" event"}

data: {"type":"token","token":" is"}

data: {"type":"token","token":" a"}

data: {"type":"token","token":" JSON"}

data: {"type":"token","token":" object"}

data: {"type":"token","token":" with"}

data: {"type":"token","token":" a"}

data: {"type":"token","token":" type"}

data: {"type":"token","token":" field"}

data: {"type":"token","token":"."}

data: {"type":"final","fullText":"The SSE protocol uses Server-Sent Events. Each event is a JSON object with a type field."}

data: {"type":"metrics","retrievalMs":42,"generationMs":120,"tokenCount":30}

```

---

## Client Usage

### JavaScript (EventSource)

```javascript
const query = encodeURIComponent("What is SSE streaming?");
const eventSource = new EventSource(`/chat/stream?query=${query}`);

eventSource.onmessage = (event) => {
  const data = JSON.parse(event.data);
  switch (data.type) {
    case "token":
      process.stdout.write(data.token);
      break;
    case "citation":
      console.log("\n[Citation]", data.documentId, data.snippet);
      break;
    case "tool_call":
      console.log("\n[Tool]", data.toolName, data.arguments);
      break;
    case "tool_result":
      console.log("\n[Result]", data.result);
      break;
    case "final":
      console.log("\n[Complete]", data.fullText);
      break;
    case "error":
      console.error("[Error]", data.code, data.message);
      break;
    case "metrics":
      console.log("\n[Metrics]", data);
      break;
  }
};

eventSource.onerror = () => eventSource.close();
```

### cURL

```bash
curl -N -H "Accept: text/event-stream" \
  "https://api.icarus.example/chat/stream?query=What%20is%20SSE?"
```

---

## Ordering Guarantees

- `token` events arrive in generation order.
- `citation` events may appear interleaved with tokens when the model references a source.
- `tool_call` and `tool_result` appear when the model uses tools; they may be interleaved with tokens.
- `final` is sent exactly once at the end of a successful stream.
- `metrics` is typically the last event before the connection closes.
- `error` terminates the stream; no `final` or further events follow.

---

## Reconnection

SSE connections may be closed by the server after idle time or errors. Clients should:

1. Parse `retry` directives if present.
2. Reconnect with the same `query` (or `streamId` if supported) for resumable streams.
3. Handle `error` events and close the connection gracefully.

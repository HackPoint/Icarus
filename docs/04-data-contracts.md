# Data Contracts

This document defines the key request/response schemas and event types used by the Icarus API.

---

## Register Source

### RegisterSourceRequest

```json
{
  "name": "Engineering Wiki",
  "connectorType": "SharePoint",
  "settings": {
    "siteUrl": "https://contoso.sharepoint.com/sites/engineering",
    "libraryPath": "/Shared Documents",
    "tenantId": "00000000-0000-0000-0000-000000000000",
    "clientId": "00000000-0000-0000-0000-000000000001"
  },
  "schedule": "0 */6 * * *"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Human-readable source name |
| `connectorType` | string | `SharePoint`, `S3`, `FileSystem`, `Custom` |
| `settings` | object | Connector-specific configuration |
| `schedule` | string | Cron expression for periodic ingestion (optional) |

### RegisterSourceResponse

```json
{
  "sourceId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Engineering Wiki",
  "connectorType": "SharePoint",
  "status": "Registered",
  "createdAt": "2025-02-14T10:00:00Z"
}
```

---

## Chat

### ChatRequest

```json
{
  "message": "What is our policy on remote work?",
  "conversationId": "conv-abc123",
  "sourceIds": ["550e8400-e29b-41d4-a716-446655440000"],
  "maxTokens": 1024,
  "temperature": 0.7,
  "topK": 5
}
```

| Field | Type | Description |
|-------|------|-------------|
| `message` | string | User message |
| `conversationId` | string | Session identifier (optional) |
| `sourceIds` | string[] | Filter to specific sources (optional) |
| `maxTokens` | int | Max response tokens |
| `temperature` | float | LLM sampling temperature |
| `topK` | int | Number of retrieval results to use |

### ChatResponse (Non-Streaming)

```json
{
  "response": "Our remote work policy allows...",
  "conversationId": "conv-abc123",
  "citations": [
    {
      "documentId": "doc-001",
      "chunkId": "chunk-001",
      "score": 0.92,
      "snippet": "Remote work policy: Employees may work..."
    }
  ],
  "model": "llama3.2",
  "usage": {
    "promptTokens": 450,
    "completionTokens": 120
  }
}
```

---

## SSE Event Types

Chat streaming uses Server-Sent Events. Each event has a `type` and a `data` payload.

### token

Incremental text from the LLM.

```json
{
  "type": "token",
  "data": {
    "content": "Our ",
    "index": 0
  }
}
```

### citation

A retrieved document chunk used as context.

```json
{
  "type": "citation",
  "data": {
    "documentId": "doc-001",
    "chunkId": "chunk-001",
    "score": 0.92,
    "snippet": "Remote work policy: Employees may work from home up to 3 days per week.",
    "sourceName": "HR Handbook"
  }
}
```

### tool_call

LLM requesting a tool invocation (e.g., search).

```json
{
  "type": "tool_call",
  "data": {
    "id": "call-1",
    "name": "search",
    "arguments": "{\"query\": \"remote work policy\"}"
  }
}
```

### tool_result

Result of a tool call.

```json
{
  "type": "tool_result",
  "data": {
    "id": "call-1",
    "result": "[{\"documentId\": \"doc-001\", \"snippet\": \"...\"}]"
  }
}
```

### final

Stream complete; full response and metadata.

```json
{
  "type": "final",
  "data": {
    "response": "Our remote work policy allows...",
    "citations": [...],
    "usage": {
      "promptTokens": 450,
      "completionTokens": 120
    }
  }
}
```

### error

Stream error.

```json
{
  "type": "error",
  "data": {
    "code": "RETRIEVAL_FAILED",
    "message": "Vector store unavailable"
  }
}
```

### metrics

Optional performance metrics.

```json
{
  "type": "metrics",
  "data": {
    "retrievalMs": 45,
    "llmFirstTokenMs": 120,
    "totalMs": 3500
  }
}
```

---

## RetrievalResult

Returned by the retrieval service for each matching chunk.

```json
{
  "documentId": "doc-001",
  "chunkId": "chunk-001",
  "sourceId": "550e8400-e29b-41d4-a716-446655440000",
  "score": 0.92,
  "text": "Remote work policy: Employees may work from home up to 3 days per week. Requests must be approved by your manager.",
  "metadata": {
    "title": "HR Handbook - Chapter 5",
    "page": 12,
    "lastModified": "2025-01-15T09:00:00Z"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `documentId` | string | Document identifier |
| `chunkId` | string | Chunk identifier |
| `sourceId` | string | Source the document came from |
| `score` | float | Similarity or relevance score |
| `text` | string | Chunk content |
| `metadata` | object | Document/chunk metadata |

---

## NormalizedDocument

Output of the Normalizer; input to the Chunker.

```json
{
  "id": "doc-001",
  "sourceId": "550e8400-e29b-41d4-a716-446655440000",
  "contentType": "application/pdf",
  "title": "Q4 Budget Report",
  "text": "Executive Summary\n\nRevenue increased by 15%...",
  "metadata": {
    "author": "Finance Team",
    "createdAt": "2025-01-10T14:00:00Z",
    "pageCount": 24
  },
  "sections": [
    {
      "heading": "Executive Summary",
      "startOffset": 0,
      "endOffset": 250
    }
  ]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique document ID |
| `sourceId` | string | Source identifier |
| `contentType` | string | MIME type |
| `title` | string | Document title |
| `text` | string | Extracted full text |
| `metadata` | object | Arbitrary metadata |
| `sections` | array | Optional section boundaries for semantic chunking |

---

## TextChunk

Output of the Chunker; input to the Indexer.

```json
{
  "id": "chunk-001",
  "documentId": "doc-001",
  "index": 0,
  "text": "Executive Summary\n\nRevenue increased by 15% compared to Q3. Key drivers include...",
  "tokenCount": 128,
  "metadata": {
    "section": "Executive Summary",
    "page": 1
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique chunk ID |
| `documentId` | string | Parent document ID |
| `index` | int | Chunk order within document |
| `text` | string | Chunk text content |
| `tokenCount` | int | Approximate token count |
| `metadata` | object | Chunk-level metadata |

---

## Related Documentation

- [Overview](00-overview.md)
- [Architecture](01-architecture.md)
- [Local Development](02-local-dev-with-aspire.md)
- [Configuration](03-configuration.md)

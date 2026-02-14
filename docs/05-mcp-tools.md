# MCP Tools

The Icarus MCP (Model Context Protocol) Tools API exposes a set of tools for source registration, search, chat, and report generation. Tools are invoked via HTTP and return structured JSON responses.

## Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/mcp/tools` | List all available tools and their schemas |
| POST | `/mcp/tools/call` | Invoke a tool by name with arguments |

---

## Available Tools

### 1. `register_source`

Registers a new document or data source for indexing and retrieval.

**Description:** Adds a source (file, URL, or blob) to the Icarus knowledge base. The source is processed, chunked, and embedded for later retrieval.

**Input Schema:**

```json
{
  "type": "object",
  "properties": {
    "sourceId": { "type": "string", "description": "Unique identifier for the source" },
    "uri": { "type": "string", "description": "URI of the source (file path or URL)" },
    "contentType": { "type": "string", "description": "MIME type, e.g. text/plain, application/pdf" },
    "metadata": { "type": "object", "description": "Optional key-value metadata" }
  },
  "required": ["sourceId", "uri"]
}
```

**Example Request:**

```json
{
  "tool": "register_source",
  "arguments": {
    "sourceId": "doc-api-spec-001",
    "uri": "file:///docs/api-specification.md",
    "contentType": "text/markdown",
    "metadata": { "project": "icarus", "version": "1.0" }
  }
}
```

**Example Response:**

```json
{
  "success": true,
  "result": {
    "sourceId": "doc-api-spec-001",
    "status": "registered",
    "chunkCount": 42,
    "message": "Source registered and indexed successfully"
  }
}
```

---

### 2. `bootstrap_source`

Initializes or re-indexes a source, typically used for first-time setup or full refresh.

**Description:** Performs a full bootstrap of a source: fetches content, chunks it, generates embeddings, and stores in the vector store. Use when `register_source` is insufficient (e.g., re-indexing after schema changes).

**Input Schema:**

```json
{
  "type": "object",
  "properties": {
    "sourceId": { "type": "string", "description": "Unique identifier for the source" },
    "uri": { "type": "string", "description": "URI of the source" },
    "force": { "type": "boolean", "description": "If true, overwrite existing index", "default": false }
  },
  "required": ["sourceId", "uri"]
}
```

**Example Request:**

```json
{
  "tool": "bootstrap_source",
  "arguments": {
    "sourceId": "kb-internal-wiki",
    "uri": "https://wiki.internal/docs",
    "force": true
  }
}
```

**Example Response:**

```json
{
  "success": true,
  "result": {
    "sourceId": "kb-internal-wiki",
    "status": "bootstrapped",
    "chunkCount": 156,
    "durationMs": 2340
  }
}
```

---

### 3. `search`

Performs semantic or keyword search over indexed sources.

**Description:** Queries the vector store and/or keyword index to retrieve relevant chunks. Supports hybrid search (semantic + keyword) when both are configured.

**Input Schema:**

```json
{
  "type": "object",
  "properties": {
    "query": { "type": "string", "description": "Search query" },
    "topK": { "type": "integer", "description": "Maximum number of results", "default": 10 },
    "sourceIds": { "type": "array", "items": { "type": "string" }, "description": "Filter by source IDs" },
    "minScore": { "type": "number", "description": "Minimum similarity score (0-1)", "default": 0.5 }
  },
  "required": ["query"]
}
```

**Example Request:**

```json
{
  "tool": "search",
  "arguments": {
    "query": "How do I configure MCP tools?",
    "topK": 5,
    "sourceIds": ["doc-api-spec-001"],
    "minScore": 0.7
  }
}
```

**Example Response:**

```json
{
  "success": true,
  "result": {
    "results": [
      {
        "documentId": "doc-api-spec-001#chunk-3",
        "snippet": "MCP tools are invoked via POST /mcp/tools/call...",
        "score": 0.92,
        "metadata": { "chunkIndex": 3 }
      }
    ],
    "totalCount": 5
  }
}
```

---

### 4. `chat_stream`

Initiates a streaming chat session with RAG (Retrieval-Augmented Generation).

**Description:** Sends a user query, retrieves relevant context, and streams the LLM response. Supports tool calls and citations during the stream.

**Input Schema:**

```json
{
  "type": "object",
  "properties": {
    "query": { "type": "string", "description": "User message or question" },
    "conversationId": { "type": "string", "description": "Optional conversation ID for multi-turn" },
    "maxTokens": { "type": "integer", "description": "Maximum tokens to generate", "default": 1024 },
    "temperature": { "type": "number", "description": "Sampling temperature (0-2)", "default": 0.7 }
  },
  "required": ["query"]
}
```

**Example Request:**

```json
{
  "tool": "chat_stream",
  "arguments": {
    "query": "What is the SSE streaming format?",
    "conversationId": "conv-abc-123",
    "maxTokens": 512,
    "temperature": 0.5
  }
}
```

**Example Response:**

```json
{
  "success": true,
  "result": {
    "streamId": "stream-xyz-789",
    "streamUrl": "/chat/stream?streamId=stream-xyz-789",
    "message": "Stream started. Connect to streamUrl for SSE events."
  }
}
```

---

### 5. `generate_report`

Generates a structured report from a query and optional template.

**Description:** Produces a report (markdown, JSON, or custom format) by combining retrieved context with an LLM-generated summary. Useful for dashboards, summaries, and documentation.

**Input Schema:**

```json
{
  "type": "object",
  "properties": {
    "topic": { "type": "string", "description": "Report topic or question" },
    "format": { "type": "string", "enum": ["markdown", "json"], "default": "markdown" },
    "template": { "type": "string", "description": "Optional template with {{placeholders}}" },
    "maxSources": { "type": "integer", "description": "Max sources to include", "default": 20 }
  },
  "required": ["topic"]
}
```

**Example Request:**

```json
{
  "tool": "generate_report",
  "arguments": {
    "topic": "Icarus API overview and endpoints",
    "format": "markdown",
    "maxSources": 10
  }
}
```

**Example Response:**

```json
{
  "success": true,
  "result": {
    "reportId": "rpt-001",
    "content": "# Icarus API Overview\n\n## Endpoints\n- GET /mcp/tools\n- POST /mcp/tools/call\n...",
    "sourcesUsed": 8,
    "generatedAt": "2025-02-14T12:00:00Z"
  }
}
```

---

## Calling Tools via POST

**Request:**

```http
POST /mcp/tools/call HTTP/1.1
Content-Type: application/json

{
  "tool": "search",
  "arguments": {
    "query": "SSE streaming",
    "topK": 5
  }
}
```

**Response (success):**

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true,
  "result": { ... }
}
```

**Response (error):**

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "success": false,
  "error": {
    "code": "INVALID_ARGUMENTS",
    "message": "Missing required argument: query"
  }
}
```

---

## Listing Tools via GET

**Request:**

```http
GET /mcp/tools HTTP/1.1
```

**Response:**

```json
{
  "tools": [
    {
      "name": "register_source",
      "description": "Registers a new document or data source for indexing",
      "inputSchema": { ... }
    },
    {
      "name": "bootstrap_source",
      "description": "Initializes or re-indexes a source",
      "inputSchema": { ... }
    }
  ]
}
```

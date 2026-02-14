# Models

Icarus uses LLMs for generation and embedding models for retrieval. This document covers running Ollama, pulling models, embeddings, MinIO layout, and switching from stub to real models.

---

## Running Ollama

Ollama runs as a Docker container and serves local LLMs.

**Start Ollama:**

```bash
docker run -d \
  --name ollama \
  -p 11434:11434 \
  -v ollama:/root/.ollama \
  ollama/ollama
```

**Verify:**

```bash
curl http://localhost:11434/api/tags
```

---

## Pulling Models

Use the provided script to pull supported models:

```bash
./infra/scripts/pull-ollama-models.sh
```

The script pulls models in order of size (smallest first). For quick iteration, start with a small model.

---

## Supported Models

| Model | Size | Use Case | Notes |
|-------|------|----------|-------|
| tinyllama | ~600MB | Development, fast iteration | Recommended for local dev |
| llama3.2 | ~2GB | General chat, RAG | Good balance of speed and quality |
| mistral | ~4GB | Higher quality | Slower, more capable |
| codellama | ~4GB | Code-focused tasks | Use for code generation |

**Start with tinyllama for development:**

```bash
ollama pull tinyllama
```

---

## Embeddings Model

Embeddings are provided by the **embeddings-rs** Rust service.

**Development mode:** The service can return deterministic, hash-based vectors instead of real embeddings. Useful when:

- No GPU or embedding API is available
- You need reproducible tests
- You want to avoid external API calls

**Configuration:**

```yaml
# config/embeddings.yaml
mode: stub  # or "real"
stub:
  deterministic: true  # hash-based vectors for dev
```

**Real mode:** Connects to the embeddings-rs service or an external embedding API (e.g., OpenAI, local sentence-transformers).

---

## MinIO Layout for Model Artifacts

Model artifacts are stored in MinIO for versioning and distribution.

**Bucket:** `icarus-models`

**Paths:**

| Path | Contents |
|------|----------|
| `/embeddings/v1/` | Embedding model weights, configs |
| `/llm/v1/` | LLM weights, tokenizers, configs |

**Example structure:**

```
icarus-models/
├── embeddings/
│   └── v1/
│       ├── config.json
│       └── model.safetensors
└── llm/
    └── v1/
        ├── config.json
        ├── tokenizer.json
        └── model.safetensors
```

**Access via MinIO client:**

```bash
mc alias set icarus http://localhost:9000 $MINIO_ACCESS_KEY $MINIO_SECRET_KEY
mc ls icarus/icarus-models/embeddings/v1/
mc ls icarus/icarus-models/llm/v1/
```

---

## Switching from Stub to Real Model

### 1. Environment

```bash
# .env or environment
EMBEDDINGS_MODE=real
OLLAMA_BASE_URL=http://localhost:11434
```

### 2. Configuration

```yaml
# config/models.yaml
embedding:
  provider: embeddings-rs  # or openai, local
  model: all-MiniLM-L6-v2

llm:
  provider: ollama
  model: tinyllama
  baseUrl: http://localhost:11434
```

### 3. Code / Feature Flags

```csharp
// In Startup or configuration
if (config.GetValue<string>("Embedding:Mode") == "real")
{
    services.AddEmbeddingService();  // Real embeddings-rs client
}
else
{
    services.AddStubEmbeddingService();  // Hash-based deterministic
}
```

### 4. Checklist

- [ ] Ollama container running (`docker ps | grep ollama`)
- [ ] Model pulled (`ollama list` shows tinyllama or desired model)
- [ ] embeddings-rs service running (if using real embeddings)
- [ ] `EMBEDDINGS_MODE=real` and `OLLAMA_BASE_URL` set
- [ ] Health check passes: `GET /health` includes `ollama: ok`, `embeddings: ok`

### 5. Verify

```bash
# Test LLM
curl "http://localhost:5000/chat/stream?query=Hello"

# Test embeddings (internal)
curl -X POST http://localhost:5000/embed \
  -H "Content-Type: application/json" \
  -d '{"texts":["test"]}'
```

---

## Troubleshooting

| Issue | Check |
|-------|-------|
| Ollama connection refused | Container running? `docker ps` |
| Model not found | `ollama pull tinyllama` |
| Slow first request | Model loads on first use; wait 30–60s |
| Stub vs real confusion | Inspect `EMBEDDINGS_MODE` and config |
| MinIO access denied | Credentials, bucket exists, policy |

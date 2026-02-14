# Local Development with Aspire

This guide walks you through running Icarus locally using .NET Aspire for orchestration and service discovery.

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| **.NET SDK** | 10.0+ | [Download](https://dotnet.microsoft.com/download) |
| **Docker** | Latest | Required for infrastructure containers |
| **Docker Compose** | v2+ | Usually bundled with Docker Desktop |
| **Rust toolchain** | 1.70+ | For embeddings service (`rustup` recommended) |

Verify installations:

```bash
dotnet --version   # 10.0.x
docker --version
rustc --version
```

## DevContainer Setup (Optional)

Icarus includes a DevContainer for consistent development environments:

1. Open the repo in VS Code or Cursor
2. Install the **Dev Containers** extension
3. Run **Dev Containers: Reopen in Container** from the command palette

The DevContainer includes .NET 10, Docker, and Rust pre-installed.

## Running with Aspire

### 1. Start Infrastructure (Docker)

Ensure required containers are running. Aspire AppHost typically starts these via `AddDockerCompose` or `AddProject` references. If you use a separate compose file:

```bash
docker compose -f docker-compose.dev.yml up -d
```

### 2. Run the AppHost

From the repository root:

```bash
dotnet run --project src/Icarus.AppHost
```

This starts:

- The Aspire dashboard
- All referenced projects (Orchestrator API, Workers, etc.)
- Infrastructure dependencies (Postgres, CouchDB, Qdrant, etc.) if configured in AppHost

### 3. Access the Aspire Dashboard

Open **http://localhost:15888** in your browser.

The dashboard provides:

- **Resources**: All running services and their status
- **Traces**: Distributed tracing (OpenTelemetry)
- **Metrics**: Service metrics
- **Logs**: Consolidated logs from all projects

### 4. Container Ports Reference

| Service | Port(s) | Purpose |
|---------|---------|---------|
| **PostgreSQL** | 5432 | Relational database |
| **CouchDB** | 5984 | Document store (HTTP API) |
| **Qdrant** | 6333 | Vector DB (gRPC/HTTP) |
| **MinIO** | 9000, 9001 | API (9000), Console (9001) |
| **ClickHouse** | 8123 | HTTP interface |
| **Ollama** | 11434 | LLM API |
| **Aspire Dashboard** | 15888 | Orchestration UI |
| **Orchestrator API** | 5000 / 5001 | REST API (HTTP/HTTPS) |

### 5. Run Individual Services (Optional)

For focused development, you can run services individually:

```bash
# Orchestrator API only
dotnet run --project src/Icarus.Orchestrator.Api

# Worker only
dotnet run --project src/Icarus.Worker

# Embeddings (Rust) service
cargo run --manifest-path src/embeddings/Cargo.toml
```

Ensure infrastructure containers are running and connection strings are set (see [Configuration](03-configuration.md)).

---

## Troubleshooting

### Aspire dashboard not loading

- Check that port 15888 is not in use: `lsof -i :15888`
- Ensure the AppHost project starts without errors; check the terminal output

### "Connection refused" to Postgres/CouchDB/Qdrant

- Verify containers are running: `docker ps`
- Confirm ports match your `appsettings.json` or environment variables
- On macOS/Windows, use `localhost`; in Docker-in-Docker, use service names from compose

### Ollama model not found

```bash
# Pull a model (e.g., llama3.2)
ollama pull llama3.2
```

Ensure `Ollama__Model` in config matches the model name.

### Rust embeddings service fails

- Run `cargo build` in the embeddings project to surface compile errors
- Check that the service URL in config is correct (e.g., `http://localhost:8081`)

### Worker jobs stuck or not processing

- Check Worker logs in the Aspire dashboard
- Verify message queue/Redis (if used) is running
- Ensure Connector, Normalizer, Chunker, and Indexer projects are referenced and running

### CORS errors from frontend

- Add your frontend origin to `Cors__AllowedOrigins` in Orchestrator API config
- For local dev, `http://localhost:3000` or `http://localhost:5173` are common

### High memory usage

- Reduce `Chunker__MaxChunkSize` or batch sizes in Indexer
- Use a smaller Ollama model for development

---

## Next Steps

- [Configuration](03-configuration.md) — Environment variables and app settings
- [Data Contracts](04-data-contracts.md) — API schemas and examples

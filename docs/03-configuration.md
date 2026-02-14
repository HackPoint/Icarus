# Configuration Reference

This document describes how Icarus services are configured via `appsettings.json` and environment variables.

## appsettings.json Structure

Each service has its own `appsettings.json`. A typical structure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=icarus;Username=icarus;Password=secret",
    "CouchDB": "http://localhost:5984",
    "Qdrant": "http://localhost:6333",
    "MinIO": "s3://localhost:9000/icarus?accessKey=minioadmin&secretKey=minioadmin",
    "ClickHouse": "Host=localhost;Port=8123"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2"
  },
  "Embeddings": {
    "ServiceUrl": "http://localhost:8081",
    "Model": "nomic-embed-text"
  }
}
```

## Environment Variables by Service

### Orchestrator API

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__Postgres` | PostgreSQL connection | `Host=db;Database=icarus;...` |
| `ConnectionStrings__CouchDB` | CouchDB URL | `http://couchdb:5984` |
| `ConnectionStrings__Qdrant` | Qdrant URL | `http://qdrant:6333` |
| `Ollama__BaseUrl` | Ollama API base URL | `http://ollama:11434` |
| `Ollama__Model` | Default LLM model | `llama3.2` |
| `Embeddings__ServiceUrl` | Rust embeddings service URL | `http://embeddings:8081` |
| `Cors__AllowedOrigins` | Comma-separated origins | `http://localhost:3000` |
| `ASPNETCORE_URLS` | Listen URLs | `http://+:5000` |

### Worker

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__Postgres` | Job queue / state | Same as Orchestrator |
| `ConnectionStrings__CouchDB` | Document store | Same as Orchestrator |
| `ConnectionStrings__Qdrant` | Vector store | Same as Orchestrator |
| `ConnectionStrings__MinIO` | Blob storage | Same as Orchestrator |
| `Embeddings__ServiceUrl` | Embeddings service | Same as Orchestrator |
| `Worker__Concurrency` | Max parallel jobs | `4` |

### Embeddings (Rust)

| Variable | Description | Example |
|----------|-------------|---------|
| `PORT` | HTTP listen port | `8081` |
| `MODEL` | Embedding model name | `nomic-embed-text` |

## Connection Strings

### PostgreSQL

```
Host=<host>;Port=5432;Database=icarus;Username=<user>;Password=<password>;SSL Mode=Prefer
```

### CouchDB

```
http://<user>:<password>@<host>:5984
```

For local dev without auth: `http://localhost:5984`

### Qdrant

```
http://<host>:6333
```

For gRPC: `http://<host>:6334`

### MinIO (S3-compatible)

```
s3://<host>:9000/<bucket>?accessKey=<key>&secretKey=<secret>&useSSL=false
```

### ClickHouse

```
Host=<host>;Port=8123;Database=icarus
```

Or HTTP URL: `http://<host>:8123`

## How Aspire Wires Config

Aspire uses **configuration providers** to merge settings:

1. **appsettings.json** (base)
2. **appsettings.{Environment}.json** (e.g., `Development`, `Production`)
3. **Environment variables** (override JSON)
4. **Aspire service discovery** (when using `AddProject` / `AddContainer`)

When you add a project reference in AppHost:

```csharp
var orchestrator = builder.AddProject<Projects.Icarus_Orchestrator_Api>("orchestrator");
```

Aspire injects:

- **Service URLs** for other referenced projects (e.g., `http://orchestrator` in the cluster)
- **Health check endpoints**
- **Distributed tracing** (OpenTelemetry)

For infrastructure containers, Aspire can inject connection strings if you configure them in the AppHost:

```csharp
builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "secret");
```

## Overriding Settings by Environment

### Development

Use `appsettings.Development.json` or environment variables:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=icarus_dev;..."
  }
}
```

Set `ASPNETCORE_ENVIRONMENT=Development` (default when running locally).

### Staging / Production

- Use `appsettings.Production.json` for production defaults
- **Never** commit secrets; use environment variables or a secrets manager
- Example: `ConnectionStrings__Postgres` from Azure Key Vault, Kubernetes secrets, or similar

### Docker Compose

Override in `docker-compose.override.yml`:

```yaml
services:
  orchestrator:
    environment:
      - ConnectionStrings__Postgres=Host=postgres;Database=icarus;...
      - Ollama__BaseUrl=http://ollama:11434
```

## Configuration Precedence (Highest to Lowest)

1. Command-line args: `--ConnectionStrings:Postgres="..."`
2. Environment variables: `ConnectionStrings__Postgres`
3. `appsettings.{Environment}.json`
4. `appsettings.json`

# Icarus

**On-Prem RAG Platform for Enterprise Document Intelligence**

Icarus is a retrieval-augmented generation (RAG) platform designed for on-premises deployment. It combines document ingestion, semantic search, and language model inference into a unified, observable system.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Control Plane                           │
│  ┌──────────────┐  ┌──────────┐  ┌───────────────────────┐ │
│  │ Orchestrator  │  │   MCP    │  │      Workers          │ │
│  │     API       │  │  Server  │  │  (Background Jobs)    │ │
│  └──────────────┘  └──────────┘  └───────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                      Data Plane                             │
│  ┌──────────┐  ┌──────────┐  ┌─────────┐  ┌────────────┐  │
│  │ CouchDB  │  │Normalizer│  │ Chunker │  │  Indexer   │  │
│  │Connector │  │          │  │         │  │            │  │
│  └──────────┘  └──────────┘  └─────────┘  └────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                      RAG Layer                              │
│  ┌──────────────────┐  ┌──────────────────────────────────┐ │
│  │    Retrieval      │  │        RAG Pipeline              │ │
│  │  (Vector Search)  │  │  (Retrieve + Generate)           │ │
│  └──────────────────┘  └──────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                    Infrastructure                           │
│  Postgres │ CouchDB │ Qdrant │ MinIO │ ClickHouse │ Ollama │
└─────────────────────────────────────────────────────────────┘
```

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started)
- [Rust toolchain](https://rustup.rs/) (for embeddings service)
- [Node.js 20+](https://nodejs.org/) (for E2E tests)

### Using DevContainer (Recommended)

1. Open the repository in VS Code or Cursor
2. Click "Reopen in Container" when prompted
3. All dependencies are pre-configured

### Manual Setup

```bash
# Clone and enter the repository
git clone https://github.com/HackPoint/Icarus.git
cd icarus

# Restore and build .NET
dotnet restore
dotnet build

# Build Rust embeddings service
cd src/ml/embeddings-rs && cargo build && cd ../../..

# Run the Aspire AppHost (starts all services + infra)
dotnet run --project src/Icarus.AppHost
```

### Seed Sample Data

```bash
bash infra/scripts/seed-sample-data.sh
```

### Pull Ollama Models

```bash
bash infra/scripts/pull-ollama-models.sh
```

## Running Tests

```bash
# All .NET tests
dotnet test

# Unit tests only
dotnet test --filter "FullyQualifiedName~Tests.Unit"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Tests.Integration"

# Rust tests
cd src/ml/embeddings-rs && cargo test

# E2E tests (requires running services)
cd tests/e2e/playwright && npm install && npx playwright test

# Smoke tests (requires running services + k6)
k6 run tests/smoke/k6/health.js
```

## Health Check

```bash
bash infra/scripts/healthcheck.sh
```

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/sources/register` | POST | Register a data source |
| `/sources/{id}/bootstrap` | POST | Start ingestion for a source |
| `/chat/query` | POST | Non-streaming chat query |
| `/chat/stream` | GET | SSE streaming chat |
| `/mcp/tools` | GET | List MCP tools |
| `/mcp/tools/call` | POST | Call an MCP tool |
| `/reports/usage` | GET | Usage analytics report |
| `/reports/sources` | GET | Sources analytics report |
| `/reports/queries` | GET | Query analytics report |

## Documentation

See the [docs/](docs/) directory for comprehensive documentation:

- [Overview](docs/00-overview.md)
- [Architecture](docs/01-architecture.md)
- [Local Dev with Aspire](docs/02-local-dev-with-aspire.md)
- [Configuration](docs/03-configuration.md)
- [Data Contracts](docs/04-data-contracts.md)
- [MCP Tools](docs/05-mcp-tools.md)
- [SSE Streaming](docs/06-sse.md)
- [Testing](docs/07-testing.md)
- [Models](docs/08-models.md)
- [Prompts](docs/09-prompts.md)

## Project Structure

```
icarus/
├── .devcontainer/          # DevContainer configuration
├── .github/workflows/      # CI/CD pipelines
├── docs/                   # Documentation
├── infra/scripts/          # Infrastructure scripts
├── src/
│   ├── Icarus.AppHost/     # Aspire orchestrator
│   ├── Icarus.ServiceDefaults/  # Shared service config
│   ├── controlplane/       # API, MCP, Workers
│   ├── dataplane/          # Connectors, Normalizer, Chunker, Indexer
│   ├── rag/                # Retrieval + RAG Pipeline
│   ├── analytics/          # Reports API
│   └── ml/embeddings-rs/   # Rust embeddings service
└── tests/
    ├── e2e/playwright/     # End-to-end tests
    ├── smoke/k6/           # Smoke/load tests
    └── snapshots/          # Verify.Xunit snapshots
```

## License

Proprietary. All rights reserved.

#!/usr/bin/env bash
set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

check_http() {
  local url="$1"
  if curl -sf --connect-timeout 3 "$url" > /dev/null 2>&1; then
    echo -e "${GREEN}UP${NC}"
    return 0
  else
    echo -e "${RED}DOWN${NC}"
    return 1
  fi
}

check_postgres() {
  if command -v pg_isready > /dev/null 2>&1; then
    if pg_isready -h localhost -p 5432 -U icarus > /dev/null 2>&1; then
      echo -e "${GREEN}UP${NC}"
      return 0
    fi
  fi
  # Fallback: try psql connection
  if PGPASSWORD=icarus_dev psql -h localhost -p 5432 -U icarus -d icarus -c "SELECT 1" > /dev/null 2>&1; then
    echo -e "${GREEN}UP${NC}"
    return 0
  fi
  echo -e "${RED}DOWN${NC}"
  return 1
}

echo "=== Icarus Infrastructure Health Check ==="
echo ""

up_count=0
down_count=0

run_check() {
  local name="$1"
  shift
  printf "  %-24s " "${name}"
  if "$@"; then
    ((up_count++)) || true
  else
    ((down_count++)) || true
  fi
}

run_check "Orchestrator API (5000):" check_http "http://localhost:5000/health"
run_check "MCP Server (5010):" check_http "http://localhost:5010/health"
run_check "Reports API (5020):" check_http "http://localhost:5020/health"
run_check "Embeddings (8900):" check_http "http://localhost:8900/health"
run_check "Postgres (5432):" check_postgres
run_check "CouchDB (5984):" check_http "http://admin:admin@localhost:5984/"
run_check "Qdrant (6333):" check_http "http://localhost:6333/"
run_check "MinIO (9000):" check_http "http://localhost:9000/minio/health/live"
run_check "ClickHouse (8123):" check_http "http://localhost:8123/ping"
run_check "Ollama (11434):" check_http "http://localhost:11434/api/tags"

echo ""
echo "=== Summary ==="
echo "  UP:   ${up_count}"
echo "  DOWN: ${down_count}"
echo ""

if [[ $down_count -gt 0 ]]; then
  exit 1
fi

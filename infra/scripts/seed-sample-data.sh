#!/usr/bin/env bash
set -euo pipefail

COUCHDB_URL="http://admin:admin@localhost:5984"
POSTGRES_URL="postgres://icarus:icarus_dev@localhost:5432/icarus"

echo "=== Icarus Sample Data Seeding ==="

# --- CouchDB ---
echo ""
echo "[1/4] Creating CouchDB database 'icarus-docs'..."
if curl -sf -X PUT "${COUCHDB_URL}/icarus-docs" > /dev/null 2>&1; then
  echo "  ✓ Database created (or already exists)"
else
  echo "  ✗ Failed to create database. Is CouchDB running at localhost:5984?"
  exit 1
fi

echo ""
echo "[2/4] Inserting sample documents into CouchDB..."
for doc_id in doc-001 doc-002 doc-003; do
  case "$doc_id" in
    doc-001)
      body='{"title":"Icarus Document Ingestion","description":"Automated document ingestion from multiple sources including S3, HTTP, and local filesystems. Supports PDF, DOCX, and plain text.","feature":"ingestion","version":1}'
      ;;
    doc-002)
      body='{"title":"Icarus RAG Pipeline","description":"Retrieval-augmented generation pipeline with vector search via Qdrant. Embeddings powered by Ollama. Supports hybrid search.","feature":"rag","version":1}'
      ;;
    doc-003)
      body='{"title":"Icarus Report Generation","description":"Automated report generation with templating. Exports to PDF and HTML. Integrates with ClickHouse for analytics.","feature":"reports","version":1}'
      ;;
  esac
  if curl -sf -X PUT "${COUCHDB_URL}/icarus-docs/${doc_id}" \
    -H "Content-Type: application/json" \
    -d "$body" > /dev/null 2>&1; then
    echo "  ✓ Inserted ${doc_id}"
  else
    echo "  ✗ Failed to insert ${doc_id}"
    exit 1
  fi
done

# --- Postgres ---
echo ""
echo "[3/4] Creating data_sources table in Postgres..."
if ! PGPASSWORD=icarus_dev psql -h localhost -p 5432 -U icarus -d icarus -v ON_ERROR_STOP=1 <<'SQL'
CREATE TABLE IF NOT EXISTS data_sources (
  id SERIAL PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  source_type VARCHAR(50) NOT NULL,
  config JSONB,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Clear existing sample data to avoid duplicates on re-run
DELETE FROM data_sources WHERE name IN ('Sample S3 Bucket', 'Sample HTTP Endpoint');

INSERT INTO data_sources (name, source_type, config) VALUES
  ('Sample S3 Bucket', 's3', '{"bucket": "icarus-docs", "region": "us-east-1"}'),
  ('Sample HTTP Endpoint', 'http', '{"url": "https://docs.example.com", "auth": "none"}');
SQL
then
  echo "  ✗ Failed to connect to Postgres. Is it running at localhost:5432?"
  exit 1
fi

echo "  ✓ Table created and sample rows inserted"

echo ""
echo "[4/4] Verifying seed data..."
COUCH_DOCS=$(curl -sf "${COUCHDB_URL}/icarus-docs/_all_docs" 2>/dev/null | grep -o '"id":"[^"]*"' | wc -l | tr -d ' ') || COUCH_DOCS="0"
echo "  ✓ CouchDB: ${COUCH_DOCS} documents in icarus-docs"
PG_ROWS=$(PGPASSWORD=icarus_dev psql -h localhost -p 5432 -U icarus -d icarus -t -A -c "SELECT COUNT(*) FROM data_sources;" 2>/dev/null) || PG_ROWS="0"
echo "  ✓ Postgres: ${PG_ROWS} rows in data_sources"

echo ""
echo "=== Sample data seeding complete ==="

#!/usr/bin/env bash
set -euo pipefail

OLLAMA_URL="http://localhost:11434"
MODEL_NAME="tinyllama"

echo "=== Ollama Model Pull: ${MODEL_NAME} ==="

echo ""
echo "[1/3] Checking if Ollama is running..."
if ! curl -sf "${OLLAMA_URL}/api/tags" > /dev/null 2>&1; then
  echo "  ✗ Ollama is not running. Start it with: ollama serve"
  echo "  Or run: ollama"
  exit 1
fi
echo "  ✓ Ollama is running"

echo ""
echo "[2/3] Pulling ${MODEL_NAME} model..."
if curl -sf -X POST "${OLLAMA_URL}/api/pull" \
  -H "Content-Type: application/json" \
  -d "{\"name\": \"${MODEL_NAME}\"}" > /dev/null 2>&1; then
  echo "  ✓ Pull request sent (model may still be downloading)"
else
  echo "  ✗ Failed to initiate pull"
  exit 1
fi

# Give it a moment to start/complete for small models
echo "  Waiting for pull to complete..."
sleep 3

echo ""
echo "[3/3] Verifying model is available..."
if curl -sf "${OLLAMA_URL}/api/tags" | grep -q "\"name\":\"${MODEL_NAME}\""; then
  echo "  ✓ ${MODEL_NAME} is available"
else
  echo "  ⚠ ${MODEL_NAME} may still be downloading. Check with: ollama list"
  echo "  Or: curl ${OLLAMA_URL}/api/tags"
fi

echo ""
echo "=== Done ==="
echo ""
echo "To pull larger models, run:"
echo "  ollama pull llama3.2        # ~2GB, good balance"
echo "  ollama pull llama3.2:3b    # ~2GB 3B variant"
echo "  ollama pull mistral        # ~4GB, strong performance"
echo "  ollama pull codellama      # ~4GB, code-focused"
echo ""
echo "Or use the API:"
echo "  curl -X POST ${OLLAMA_URL}/api/pull -d '{\"name\": \"llama3.2\"}'"
echo ""

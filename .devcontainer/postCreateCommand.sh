#!/usr/bin/env bash
set -euo pipefail

echo "=== Icarus Dev Environment Setup ==="

# Restore .NET packages
echo "Restoring .NET packages..."
dotnet restore

# Build solution
echo "Building solution..."
dotnet build --no-restore

# Install Playwright dependencies (for e2e tests)
if command -v npx &> /dev/null; then
  echo "Installing Playwright test dependencies..."
  cd tests/e2e/playwright && npm install && cd ../../..
fi

# Build Rust embeddings service
if command -v cargo &> /dev/null; then
  echo "Building Rust embeddings service..."
  cd src/ml/embeddings-rs && cargo build && cd ../../..
fi

echo "=== Dev environment ready! ==="
echo "Run 'dotnet run --project src/Icarus.AppHost' to start the Aspire orchestrator."

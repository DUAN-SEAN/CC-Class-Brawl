#!/bin/bash
# Start Claude Code monitoring stack
# Usage: bash monitoring/start.sh

set -e

echo "=== Claude Code Monitoring Stack ==="

# Check Docker
if ! command -v docker &>/dev/null; then
    echo "ERROR: Docker is not installed or not on PATH."
    echo "Install Docker Desktop: https://www.docker.com/products/docker-desktop/"
    exit 1
fi

if ! docker info &>/dev/null 2>&1; then
    echo "ERROR: Docker daemon is not running. Start Docker Desktop first."
    exit 1
fi

MONITORING_DIR="$(cd "$(dirname "$0")" && pwd)"

# Create .env from example if missing
if [ ! -f "$MONITORING_DIR/.env" ]; then
    cp "$MONITORING_DIR/.env.example" "$MONITORING_DIR/.env"
    echo "Created monitoring/.env from .env.example"
    echo "  -> Review and change the Grafana admin password if needed."
fi

# Start the stack
cd "$MONITORING_DIR"
docker compose up -d

echo ""
echo "=== Stack Started ==="
echo "  Grafana:       http://localhost:3000  (admin / admin)"
echo "  Prometheus:    http://localhost:9090"
echo "  OTel gRPC:     localhost:4317"
echo "  OTel HTTP:     localhost:4318"
echo ""
echo "Set Claude Code env vars to start sending telemetry."
echo "Run 'bash monitoring/status.sh' to check health."

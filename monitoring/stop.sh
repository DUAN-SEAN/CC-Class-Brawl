#!/bin/bash
# Stop Claude Code monitoring stack
# Usage: bash monitoring/stop.sh

set -e
MONITORING_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$MONITORING_DIR"

echo "Stopping Claude Code monitoring stack..."
docker compose down

echo ""
echo "Stack stopped. Data preserved in Docker volumes."
echo "Run 'bash monitoring/destroy.sh' to remove all data."

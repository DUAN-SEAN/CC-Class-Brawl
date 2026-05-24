#!/bin/bash
# Remove monitoring stack AND all data volumes
# Usage: bash monitoring/destroy.sh

set -e
MONITORING_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$MONITORING_DIR"

echo "WARNING: This will delete all monitoring data (metrics, logs, dashboards)."
read -p "Continue? (y/N) " confirm
if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo "Aborted."
    exit 0
fi

docker compose down -v
echo "Stack and volumes removed."

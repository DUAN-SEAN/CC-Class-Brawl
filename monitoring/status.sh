#!/bin/bash
# Check monitoring stack health
# Usage: bash monitoring/status.sh

MONITORING_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$MONITORING_DIR"

echo "=== Monitoring Stack Status ==="
echo ""

# Check if containers are running
echo "--- Containers ---"
docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || echo "Stack not running."
echo ""

# Quick health checks
check_url() {
    local name=$1
    local url=$2
    if curl -sf -o /dev/null -m 3 "$url" 2>/dev/null; then
        echo "  $name: OK"
    else
        echo "  $name: UNREACHABLE"
    fi
}

echo "--- Health ---"
check_url "Grafana"    "http://localhost:3000/api/health"
check_url "Prometheus"  "http://localhost:9090/-/healthy"
check_url "OTel (gRPC)" "http://localhost:4317" 2>/dev/null || echo "  OTel (gRPC): (cannot check gRPC via HTTP, this is normal)"
echo ""
echo "Dashboard: http://localhost:3000/d/claude-code-obs"

#!/bin/bash
# Load Testing Script
# Run this to test the performance of your running API
# Prerequisites: API must already be running (use start.sh first)

if [ "$1" = "--help" ] || [ "$1" = "-h" ]; then
    echo "Load Testing Script for ReverseSentence API"
    echo ""
    echo "Usage:"
    echo "  ./load-test.sh          Run load tests against running API"
    echo "  ./load-test.sh --help   Show this help message"
    echo ""
    echo "Prerequisites:"
    echo "  - API must be running (docker compose up -d)"
    echo "  - Use start.sh to start the application first"
    echo ""
    exit 0
fi

echo "ReverseSentence Load Testing"
echo "================================"
echo ""

# Check if Docker is running
if ! docker ps > /dev/null 2>&1; then
    echo "[ERROR] Docker is not running. Please start Docker."
    exit 1
fi
echo "[OK] Docker is running"

# Verify API is running
echo ""
echo "[CHECK] Verifying API is running..."
API_RUNNING=$(docker ps --filter "name=reversesentence-api" --format "{{.Names}}")
if [ "$API_RUNNING" = "reversesentence-api" ]; then
    echo "[OK] API container is running"
else
    echo "[ERROR] API is not running."
    echo "Start the application first with: ./start.sh"
    exit 1
fi

# Health check
echo ""
echo "[HEALTH] Checking API health..."
MAX_RETRIES=6
RETRY_COUNT=0
HEALTHY=false

while [ $RETRY_COUNT -lt $MAX_RETRIES ] && [ "$HEALTHY" = false ]; do
    if curl -f http://localhost:5001/health > /dev/null 2>&1; then
        echo "[OK] API is healthy and responding"
        HEALTHY=true
    else
        RETRY_COUNT=$((RETRY_COUNT + 1))
        if [ $RETRY_COUNT -lt $MAX_RETRIES ]; then
            echo "[RETRY] API not ready yet, retrying... ($RETRY_COUNT/$MAX_RETRIES)"
            sleep 5
        else
            echo "[WARN] API health check failed after $MAX_RETRIES attempts, but continuing..."
        fi
    fi
done

echo ""
echo "=================================================="
echo "Running Load Tests"
echo "=================================================="
echo ""

# Menu
echo "Select a test to run:"
echo "1. Rate Limiting Load Test (3 min, gradual ramp 0->50 users)"
echo "2. Spike Test (1 min, sudden spike to 500 users)"
echo "3. Both tests"
echo "4. Exit"
echo ""
read -p "Enter choice (1-4): " choice

case $choice in
    1)
        echo ""
        echo "[TEST] Running Rate Limiting Load Test..."
        echo ""
        docker compose run --rm k6 run /scripts/rate-limiting-load.js
        ;;
    2)
        echo ""
        echo "[TEST] Running Spike Test..."
        echo ""
        docker compose run --rm k6 run /scripts/spike-test.js
        ;;
    3)
        echo ""
        echo "[TEST] Running Rate Limiting Load Test..."
        echo ""
        docker compose run --rm k6 run /scripts/rate-limiting-load.js
        
        echo ""
        echo "[TEST] Running Spike Test..."
        echo ""
        docker compose run --rm k6 run /scripts/spike-test.js
        ;;
    4)
        echo ""
        echo "Exiting..."
        echo ""
        exit 0
        ;;
    *)
        echo ""
        echo "[ERROR] Invalid choice. Exiting."
        echo ""
        exit 1
        ;;
esac

echo ""
echo "=================================================="
echo "[COMPLETE] Load testing complete!"
echo "=================================================="
echo ""

echo "Quick Tips:"
echo "- Review results above for performance metrics"
echo "- Check API logs: docker compose logs -f api"
echo "- Stop containers: docker compose down"
echo "- Restart API: ./start.sh"

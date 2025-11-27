#!/bin/bash
# ReverseSentence API - Startup Script
# Builds and starts the application with optional load testing

echo "ReverseSentence API - Startup"
echo "=============================="
echo ""

# Check if Docker is running
if ! docker ps > /dev/null 2>&1; then
    echo "[ERROR] Docker is not running. Please start Docker."
    exit 1
fi
echo "[OK] Docker is running"

# Step 1: Check if image needs building
echo ""
echo "[CHECK] Checking for existing API image..."
IMAGE_EXISTS=$(docker images --format "{{.Repository}}:{{.Tag}}" | grep "reversesentence-api")

if [ -n "$IMAGE_EXISTS" ]; then
    echo "[OK] API image already exists"
    echo ""
    echo "Do you want to rebuild the image?"
    echo "  Choose YES if you've made code changes"
    echo "  Choose NO to use existing image (faster)"
    echo ""
    read -p "Rebuild image? (y/N): " rebuild
    
    if [ "$rebuild" = "y" ] || [ "$rebuild" = "Y" ]; then
        echo ""
        echo "[BUILD] Rebuilding Docker image..."
        if ! docker compose build --no-cache; then
            echo "[ERROR] Docker build failed"
            exit 1
        fi
        echo "[OK] Image rebuilt successfully"
    else
        echo ""
        echo "[SKIP] Using existing image"
    fi
else
    echo "[BUILD] No existing image found. Building for first time..."
    if ! docker compose build; then
        echo "[ERROR] Docker build failed"
        exit 1
    fi
    echo "[OK] Image built successfully"
fi

# Step 2: Start containers
echo ""
echo "[START] Starting containers..."
if ! docker compose up -d; then
    echo "[ERROR] Failed to start containers"
    exit 1
fi
echo "[OK] Containers started"

# Step 3: Wait for API to be healthy
echo ""
echo "[WAIT] Waiting for API to be ready..."
MAX_RETRIES=12
RETRY_COUNT=0
HEALTHY=false

while [ $RETRY_COUNT -lt $MAX_RETRIES ] && [ "$HEALTHY" = false ]; do
    sleep 5
    if curl -f http://localhost:5001/health > /dev/null 2>&1; then
        echo "[OK] API is healthy and ready!"
        HEALTHY=true
    else
        RETRY_COUNT=$((RETRY_COUNT + 1))
        if [ $RETRY_COUNT -lt $MAX_RETRIES ]; then
            echo "[RETRY] API starting... ($RETRY_COUNT/$MAX_RETRIES)"
        else
            echo "[WARN] API health check timeout, but may still be starting..."
        fi
    fi
done

# Success message
echo ""
echo "=================================================="
echo "APPLICATION STARTED SUCCESSFULLY!"
echo "=================================================="

echo ""
echo "Access the API:"
echo "  Swagger UI:  http://localhost:5001/swagger"
echo "  Health:      http://localhost:5001/health"
echo "  API Base:    http://localhost:5001/api"

echo ""
echo "Test Credentials:"
echo "  Username: admin"
echo "  Password: Admin123!"

# Step 4: Optional load testing
echo ""
echo "=================================================="
read -p "Do you want to run load tests? (y/N): " loadTest

if [ "$loadTest" = "y" ] || [ "$loadTest" = "Y" ]; then
    echo ""
    echo "[LOAD TEST] Starting load testing menu..."
    
    echo ""
    echo "Select a test to run:"
    echo "1. Rate Limiting Load Test (3 min, gradual ramp 0->50 users)"
    echo "2. Spike Test (1 min, sudden spike to 500 users)"
    echo "3. Both tests"
    echo "4. Skip load testing"
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
            echo "[SKIP] Skipping load tests"
            ;;
        *)
            echo ""
            echo "[SKIP] Invalid choice, skipping load tests"
            ;;
    esac
else
    echo ""
    echo "[SKIP] Skipping load tests"
fi

# Final instructions
echo ""
echo "=================================================="
echo "Quick Commands:"
echo "  View logs:        docker compose logs -f api"
echo "  Stop app:         docker compose down"
echo "  Restart:          docker compose restart api"
echo "  Load test later:  ./load-test.sh"
echo "=================================================="

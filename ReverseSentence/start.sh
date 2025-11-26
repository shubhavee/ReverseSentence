#!/bin/bash

# Quick Start Script for Reverse Sentence API
# This script starts MongoDB and the API in one command

echo "========================================"
echo "  Reverse Sentence API - Quick Start"
echo "========================================"
echo ""

# Check if Docker is running
echo "[1/4] Checking Docker..."
if ! docker info > /dev/null 2>&1; then
    echo "? Docker is not running. Please start Docker."
    exit 1
fi
echo "? Docker is running"

# Start MongoDB
echo ""
echo "[2/4] Starting MongoDB..."
docker-compose up -d
if [ $? -eq 0 ]; then
    echo "? MongoDB started successfully"
    echo "  Container: reversesentence-mongodb"
    echo "  Port: 27017"
else
    echo "? Failed to start MongoDB"
    exit 1
fi

# Wait for MongoDB to be ready
echo ""
echo "[3/4] Waiting for MongoDB to be ready..."
sleep 3
echo "? MongoDB is ready"

# Start the API
echo ""
echo "[4/4] Starting API..."
echo "  Running: dotnet run --launch-profile https"
echo ""
echo "========================================"
echo "  API is starting..."
echo ""
echo "  Once started, open your browser to:"
echo "  https://localhost:7017/swagger"
echo ""
echo "  (You may see a certificate warning - click 'Advanced' and 'Proceed')"
echo "========================================"
echo ""

dotnet run --launch-profile https

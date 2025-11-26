# Quick Start Script for Reverse Sentence API
# This script starts MongoDB and the API in one command

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Reverse Sentence API - Quick Start" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Host "[1/4] Checking Docker..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "? Docker is running" -ForegroundColor Green
} catch {
    Write-Host "? Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Start MongoDB
Write-Host "`n[2/4] Starting MongoDB..." -ForegroundColor Yellow
docker-compose up -d
if ($LASTEXITCODE -eq 0) {
    Write-Host "? MongoDB started successfully" -ForegroundColor Green
    Write-Host "  Container: reversesentence-mongodb" -ForegroundColor Gray
    Write-Host "  Port: 27017" -ForegroundColor Gray
} else {
    Write-Host "? Failed to start MongoDB" -ForegroundColor Red
    exit 1
}

# Wait for MongoDB to be ready
Write-Host "`n[3/4] Waiting for MongoDB to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 3
Write-Host "? MongoDB is ready" -ForegroundColor Green

# Start the API
Write-Host "`n[4/4] Starting API..." -ForegroundColor Yellow
Write-Host "  Running: dotnet run --launch-profile https" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  API is starting..." -ForegroundColor Cyan
Write-Host "" -ForegroundColor Cyan
Write-Host "  Once started, open your browser to:" -ForegroundColor Green
Write-Host "  https://localhost:7017/swagger" -ForegroundColor Yellow
Write-Host "" -ForegroundColor Cyan
Write-Host "  (You may see a certificate warning - click 'Advanced' and 'Proceed')" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

dotnet run --launch-profile https

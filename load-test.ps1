# Load Testing Script
# Run this to test the performance of your running API
# Prerequisites: API must already be running (use start.ps1 first)

param(
    [switch]$Help
)

if ($Help) {
    Write-Host "Load Testing Script for ReverseSentence API`n" -ForegroundColor Cyan
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\load-test.ps1          Run load tests against running API"
    Write-Host "  .\load-test.ps1 -Help    Show this help message`n"
    Write-Host "Prerequisites:" -ForegroundColor Yellow
    Write-Host "  - API must be running (docker compose up -d)"
    Write-Host "  - Use start.ps1 to start the application first`n"
    exit 0
}

Write-Host "ReverseSentence Load Testing" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

# Check if Docker is running
try {
    docker ps | Out-Null
    Write-Host "[OK] Docker is running" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Verify API is running
Write-Host "`n[CHECK] Verifying API is running..." -ForegroundColor Yellow
$apiRunning = docker ps --filter "name=reversesentence-api" --format "{{.Names}}"
if ($apiRunning -eq "reversesentence-api") {
    Write-Host "[OK] API container is running" -ForegroundColor Green
} else {
    Write-Host "[ERROR] API is not running." -ForegroundColor Red
    Write-Host "Start the application first with: .\start.ps1" -ForegroundColor Yellow
    exit 1
}

# Health check
Write-Host "`n[HEALTH] Checking API health..." -ForegroundColor Yellow
$maxRetries = 6
$retryCount = 0
$healthy = $false

while ($retryCount -lt $maxRetries -and -not $healthy) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5001/health" -TimeoutSec 5 -ErrorAction Stop
        Write-Host "[OK] API is healthy and responding" -ForegroundColor Green
        $healthy = $true
    } catch {
        $retryCount++
        if ($retryCount -lt $maxRetries) {
            Write-Host "[RETRY] API not ready yet, retrying... ($retryCount/$maxRetries)" -ForegroundColor Yellow
            Start-Sleep -Seconds 5
        } else {
            Write-Host "[WARN] API health check failed after $maxRetries attempts, but continuing..." -ForegroundColor Yellow
        }
    }
}

Write-Host "`n" + "="*50 -ForegroundColor Cyan
Write-Host "Running Load Tests" -ForegroundColor Cyan
Write-Host "="*50 + "`n" -ForegroundColor Cyan

# Menu
Write-Host "Select a test to run:" -ForegroundColor Yellow
Write-Host "1. Rate Limiting Load Test (3 min, gradual ramp 0->50 users)"
Write-Host "2. Spike Test (1 min, sudden spike to 500 users)"
Write-Host "3. Both tests"
Write-Host "4. Exit`n"

$choice = Read-Host "Enter choice (1-4)"

switch ($choice) {
    "1" {
        Write-Host "`n[TEST] Running Rate Limiting Load Test...`n" -ForegroundColor Green
        docker compose run --rm k6 run /scripts/rate-limiting-load.js
    }
    "2" {
        Write-Host "`n[TEST] Running Spike Test...`n" -ForegroundColor Green
        docker compose run --rm k6 run /scripts/spike-test.js
    }
    "3" {
        Write-Host "`n[TEST] Running Rate Limiting Load Test...`n" -ForegroundColor Green
        docker compose run --rm k6 run /scripts/rate-limiting-load.js
        
        Write-Host "`n[TEST] Running Spike Test...`n" -ForegroundColor Green
        docker compose run --rm k6 run /scripts/spike-test.js
    }
    "4" {
        Write-Host "`nExiting...`n" -ForegroundColor Yellow
        exit 0
    }
    default {
        Write-Host "`n[ERROR] Invalid choice. Exiting.`n" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n" + "="*50 -ForegroundColor Cyan
Write-Host "[COMPLETE] Load testing complete!" -ForegroundColor Green
Write-Host "="*50 + "`n" -ForegroundColor Cyan

Write-Host "Quick Tips:" -ForegroundColor Yellow
Write-Host "- Review results above for performance metrics"
Write-Host "- Check API logs: docker compose logs -f api"
Write-Host "- Stop containers: docker compose down"
Write-Host "- Restart API: .\start.ps1"

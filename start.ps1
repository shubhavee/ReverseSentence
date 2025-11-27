# ReverseSentence API - Startup Script
# Builds and starts the application with optional load testing

Write-Host "ReverseSentence API - Startup" -ForegroundColor Cyan
Write-Host "==============================`n" -ForegroundColor Cyan

# Check if Docker is running
try {
    docker ps | Out-Null
    Write-Host "[OK] Docker is running" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Step 1: Check if image needs building
Write-Host "`n[CHECK] Checking for existing API image..." -ForegroundColor Yellow
$imageExists = docker images --format "{{.Repository}}:{{.Tag}}" | Select-String "reversesentence-api"

if ($imageExists) {
    Write-Host "[OK] API image already exists" -ForegroundColor Green
    Write-Host "`nDo you want to rebuild the image?" -ForegroundColor Yellow
    Write-Host "  Choose YES if you've made code changes" -ForegroundColor Gray
    Write-Host "  Choose NO to use existing image (faster)" -ForegroundColor Gray
    
    $rebuild = Read-Host "`nRebuild image? (y/N)"
    
    if ($rebuild -eq 'y' -or $rebuild -eq 'Y') {
        Write-Host "`n[BUILD] Rebuilding Docker image..." -ForegroundColor Yellow
        docker compose build --no-cache
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] Docker build failed" -ForegroundColor Red
            exit 1
        }
        Write-Host "[OK] Image rebuilt successfully" -ForegroundColor Green
    } else {
        Write-Host "`n[SKIP] Using existing image" -ForegroundColor Yellow
    }
} else {
    Write-Host "[BUILD] No existing image found. Building for first time..." -ForegroundColor Yellow
    docker compose build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Docker build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] Image built successfully" -ForegroundColor Green
}

# Step 2: Start containers
Write-Host "`n[START] Starting containers..." -ForegroundColor Yellow
docker compose up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Failed to start containers" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Containers started" -ForegroundColor Green

# Step 3: Wait for API to be healthy
Write-Host "`n[WAIT] Waiting for API to be ready..." -ForegroundColor Yellow
$maxRetries = 12
$retryCount = 0
$healthy = $false

while ($retryCount -lt $maxRetries -and -not $healthy) {
    Start-Sleep -Seconds 5
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5001/health" -TimeoutSec 3 -ErrorAction Stop
        Write-Host "[OK] API is healthy and ready!" -ForegroundColor Green
        $healthy = $true
    } catch {
        $retryCount++
        if ($retryCount -lt $maxRetries) {
            Write-Host "[RETRY] API starting... ($retryCount/$maxRetries)" -ForegroundColor Yellow
        } else {
            Write-Host "[WARN] API health check timeout, but may still be starting..." -ForegroundColor Yellow
        }
    }
}

# Success message
Write-Host "`n" + "="*50 -ForegroundColor Green
Write-Host "APPLICATION STARTED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "="*50 -ForegroundColor Green

Write-Host "`nAccess the API:" -ForegroundColor Cyan
Write-Host "  Swagger UI:  http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  Health:      http://localhost:5001/health" -ForegroundColor White
Write-Host "  API Base:    http://localhost:5001/api" -ForegroundColor White

Write-Host "`nTest Credentials:" -ForegroundColor Cyan
Write-Host "  Username: admin" -ForegroundColor White
Write-Host "  Password: Admin123!" -ForegroundColor White

# Step 4: Optional load testing
Write-Host "`n" + "="*50 -ForegroundColor Cyan
$loadTest = Read-Host "`nDo you want to run load tests? (y/N)"

if ($loadTest -eq 'y' -or $loadTest -eq 'Y') {
    Write-Host "`n[LOAD TEST] Starting load testing menu..." -ForegroundColor Green
    
    Write-Host "`nSelect a test to run:" -ForegroundColor Yellow
    Write-Host "1. Rate Limiting Load Test (3 min, gradual ramp 0->50 users)"
    Write-Host "2. Spike Test (1 min, sudden spike to 500 users)"
    Write-Host "3. Both tests"
    Write-Host "4. Skip load testing`n"
    
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
            Write-Host "`n[SKIP] Skipping load tests" -ForegroundColor Yellow
        }
        default {
            Write-Host "`n[SKIP] Invalid choice, skipping load tests" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "`n[SKIP] Skipping load tests" -ForegroundColor Yellow
}

# Final instructions
Write-Host "`n" + "="*50 -ForegroundColor Cyan
Write-Host "Quick Commands:" -ForegroundColor Cyan
Write-Host "  View logs:        docker compose logs -f api" -ForegroundColor White
Write-Host "  Stop app:         docker compose down" -ForegroundColor White
Write-Host "  Restart:          docker compose restart api" -ForegroundColor White
Write-Host "  Load test later:  .\load-test.ps1" -ForegroundColor White
Write-Host "="*50 -ForegroundColor Cyan

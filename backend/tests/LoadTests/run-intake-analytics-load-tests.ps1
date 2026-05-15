# QA-06: Load test runner — intake + analytics
# Usage:
#   .\run-intake-analytics-load-tests.ps1                 # smoke
#   .\run-intake-analytics-load-tests.ps1 -Mode full      # full load
#   .\run-intake-analytics-load-tests.ps1 -BaseUrl http://localhost:5000 -Mode smoke

param(
    [string]$BaseUrl  = "http://localhost:5000",
    [string]$Mode     = "smoke",
    [string]$TenantId = "qa-load-tenant",
    [int]$SmokeVUs    = 3,
    [string]$SmokeDuration = "30s",
    [switch]$AutoStartApi
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Script     = Join-Path $ScriptDir "intake-analytics-load.js"
$ResultsDir = Join-Path $ScriptDir "results"
if (-not (Test-Path $ResultsDir)) { New-Item -ItemType Directory -Path $ResultsDir | Out-Null }

$Timestamp  = Get-Date -Format "yyyyMMdd-HHmmss"
$ResultFile = Join-Path $ResultsDir "intake-analytics-load-$Timestamp.json"

# Verify k6 is available
if (-not (Get-Command k6 -ErrorAction SilentlyContinue)) {
    Write-Error "k6 is not installed. Install from https://k6.io/docs/getting-started/installation/"
    exit 1
}

# Optional: auto-start API in background
$ApiProcess = $null
if ($AutoStartApi) {
    Write-Host "[QA-06] Starting API for load test..." -ForegroundColor Cyan
    $ApiProjectDir = Join-Path $ScriptDir "..\..\src\Api"
    $ApiProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project $ApiProjectDir --urls $BaseUrl" `
        -PassThru -NoNewWindow

    Write-Host "[QA-06] Waiting for API readiness at $BaseUrl/health/live ..."
    $Attempts = 0
    do {
        Start-Sleep -Seconds 2
        $Attempts++
        try {
            $resp = Invoke-WebRequest -Uri "$BaseUrl/health/live" -UseBasicParsing -TimeoutSec 3
            if ($resp.StatusCode -eq 200) { break }
        } catch { }
    } while ($Attempts -lt 30)

    if ($Attempts -ge 30) {
        Write-Error "[QA-06] API did not become ready in time."
        if ($ApiProcess) { $ApiProcess.Kill() }
        exit 1
    }
    Write-Host "[QA-06] API is ready." -ForegroundColor Green
}

# Build k6 env args
$EnvArgs = @(
    "-e", "BASE_URL=$BaseUrl",
    "-e", "TENANT_ID=$TenantId",
    "-e", "RUN_MODE=$Mode",
    "-e", "SMOKE_VUS=$SmokeVUs",
    "-e", "SMOKE_DURATION=$SmokeDuration"
)

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " QA-06: Intake + Analytics Load Test" -ForegroundColor Cyan
Write-Host " Mode:    $Mode" -ForegroundColor Cyan
Write-Host " Target:  $BaseUrl" -ForegroundColor Cyan
Write-Host " Output:  $ResultFile" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

try {
    & k6 run @EnvArgs --out "json=$ResultFile" $Script
    $ExitCode = $LASTEXITCODE
} finally {
    if ($ApiProcess -and -not $ApiProcess.HasExited) {
        $ApiProcess.Kill()
        Write-Host "[QA-06] API process stopped." -ForegroundColor Yellow
    }
}

Write-Host ""
if ($ExitCode -eq 0) {
    Write-Host "[QA-06] PASS — All thresholds met." -ForegroundColor Green
} else {
    Write-Host "[QA-06] FAIL — One or more thresholds exceeded (exit code: $ExitCode)." -ForegroundColor Red
}

Write-Host "[QA-06] Results saved to: $ResultFile" -ForegroundColor Cyan
exit $ExitCode

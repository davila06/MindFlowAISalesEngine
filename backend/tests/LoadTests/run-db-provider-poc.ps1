param(
    [ValidateSet("sqlite", "sqlserver", "postgres")]
    [string]$Provider = "sqlite",
    [string]$Mode = "smoke",
    [string]$ApiBaseUrl = "http://localhost:5078",
    [string]$ResultsDir = "./results"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Ensure-Dir {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Get-Timestamp {
    (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss")
}

function Run-K6 {
    param(
        [string]$OutputPath,
        [string]$TargetUrl,
        [string]$RunMode,
        [string]$ProviderName
    )

    $env:BASE_URL = $TargetUrl
    $env:RUN_MODE = $RunMode
    $env:DB_PROVIDER = $ProviderName

    $k6Command = Get-Command k6 -ErrorAction SilentlyContinue
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\.." )).Path
    $bundledK6 = Join-Path $repoRoot ".tools\k6\k6-v0.53.0-windows-amd64\k6.exe"

    # Reuse the existing analytics load profile for comparable measurements.
    if ($k6Command) {
        & k6 run .\analytics-advanced-load.js --summary-export $OutputPath
        return
    }

    if (Test-Path $bundledK6) {
        & $bundledK6 run .\analytics-advanced-load.js --summary-export $OutputPath
        return
    }

    throw "k6 CLI not found. Install k6 or place bundled binary at $bundledK6"
}

Ensure-Dir $ResultsDir
$timestamp = Get-Timestamp
$outFile = Join-Path $ResultsDir "db-poc-$Provider-$Mode-$timestamp.json"

Write-Step "DB Provider PoC Run"
Write-Host "Provider : $Provider"
Write-Host "Mode     : $Mode"
Write-Host "API URL  : $ApiBaseUrl"
Write-Host "Output   : $outFile"

switch ($Provider) {
    "sqlite" {
        Write-Step "SQLite baseline run"
        Run-K6 -OutputPath $outFile -TargetUrl $ApiBaseUrl -RunMode $Mode -ProviderName $Provider
    }
    "sqlserver" {
        Write-Step "SQL Server run"
        Write-Host "Prerequisite: API must be running with SQL Server connection settings."
        Run-K6 -OutputPath $outFile -TargetUrl $ApiBaseUrl -RunMode $Mode -ProviderName $Provider
    }
    "postgres" {
        Write-Step "PostgreSQL run"
        Write-Host "Prerequisite: API must be running with PostgreSQL connection settings."
        Run-K6 -OutputPath $outFile -TargetUrl $ApiBaseUrl -RunMode $Mode -ProviderName $Provider
    }
}

Write-Step "Done"
Write-Host "Result exported to: $outFile" -ForegroundColor Green

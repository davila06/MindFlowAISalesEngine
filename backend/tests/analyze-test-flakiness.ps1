# QA-18: Test Flakiness SLO Analyzer
#
# Reads TRX test result files and calculates per-test flakiness rate.
# A test is considered flaky if it passed in some runs and failed in others.
# SLO: No single test may have a flakiness rate > 5% across the analyzed window.
#
# Usage:
#   .\analyze-test-flakiness.ps1                      # scans .\TestResults\
#   .\analyze-test-flakiness.ps1 -TrxDir ./TestResults -MaxFlakiness 5

param(
    [string]$TrxDir        = "$PSScriptRoot\Api.Tests\TestResults",
    [double]$MaxFlakiness  = 5.0,       # SLO threshold: % flakiness allowed per test
    [int]$MinRuns          = 2,          # minimum runs required to assess flakiness
    [string]$OutputDir     = "$PSScriptRoot\reports",
    [switch]$Fail          # exit non-zero if SLO is breached
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }

Write-Host ""
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  QA-18: Test Flakiness SLO Analyzer" -ForegroundColor Cyan
Write-Host "  SLO: Max flakiness per test = $MaxFlakiness%" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ── Collect TRX files ──────────────────────────────────────────────────────────
$TrxFiles = Get-ChildItem -Path $TrxDir -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue
if ($TrxFiles.Count -eq 0) {
    Write-Warning "No TRX files found in $TrxDir"
    exit 0
}

Write-Host "Found $($TrxFiles.Count) TRX file(s)." -ForegroundColor Cyan

# ── Parse test results ─────────────────────────────────────────────────────────
# Key: test name; Value: @{ Passed=0; Failed=0; Total=0 }
$TestStats = @{}

foreach ($trxFile in $TrxFiles) {
    try {
        [xml]$trx = Get-Content $trxFile.FullName -Encoding UTF8
        $ns = @{ trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010" }

        $results = $trx.TestRun.Results.UnitTestResult
        if ($null -eq $results) { continue }

        foreach ($result in $results) {
            $name    = $result.testName
            $outcome = $result.outcome   # "Passed" | "Failed" | "NotExecuted"

            if (-not $TestStats.ContainsKey($name)) {
                $TestStats[$name] = @{ Passed = 0; Failed = 0; Total = 0 }
            }

            $TestStats[$name].Total++
            if ($outcome -eq "Passed")       { $TestStats[$name].Passed++ }
            elseif ($outcome -eq "Failed")   { $TestStats[$name].Failed++ }
        }
    } catch {
        Write-Warning "Could not parse $($trxFile.Name): $_"
    }
}

# ── Analyse flakiness ──────────────────────────────────────────────────────────
$FlakyTests    = @()
$SloBreached   = $false
$TotalAnalysed = 0

foreach ($name in $TestStats.Keys) {
    $stats = $TestStats[$name]
    if ($stats.Total -lt $MinRuns) { continue }
    $TotalAnalysed++

    $flakeRate = [math]::Round(($stats.Failed / $stats.Total) * 100.0, 1)

    if ($stats.Failed -gt 0 -and $stats.Passed -gt 0) {
        # Only flag as flaky if it has BOTH passing and failing runs
        $FlakyTests += [pscustomobject]@{
            TestName    = $name
            TotalRuns   = $stats.Total
            Passed      = $stats.Passed
            Failed      = $stats.Failed
            FlakeRate   = $flakeRate
            SloBreached = ($flakeRate -gt $MaxFlakiness)
        }
        if ($flakeRate -gt $MaxFlakiness) { $SloBreached = $true }
    }
}

$FlakyTests = $FlakyTests | Sort-Object FlakeRate -Descending

# ── Display report ─────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Tests analysed:  $TotalAnalysed" -ForegroundColor Cyan
Write-Host "Flaky tests:     $($FlakyTests.Count)" -ForegroundColor $(if ($FlakyTests.Count -gt 0) { "Yellow" } else { "Green" })
Write-Host ""

if ($FlakyTests.Count -gt 0) {
    $FlakyTests | Format-Table -AutoSize `
        @{N="Test Name (truncated)"; E={ $_.TestName.Substring([math]::Max(0,$_.TestName.Length-60)) }},
        @{N="Runs";    E={ $_.TotalRuns }; Width=6},
        @{N="Passed";  E={ $_.Passed };   Width=7},
        @{N="Failed";  E={ $_.Failed };   Width=7},
        @{N="Flake%";  E={ "$($_.FlakeRate)%" }; Width=8},
        @{N="SLO";     E={ if ($_.SloBreached) { "BREACH" } else { "OK" } }; Width=8}
} else {
    Write-Host "✅ No flaky tests detected across $TotalAnalysed analysed test(s)." -ForegroundColor Green
}

# ── Save JSON report ───────────────────────────────────────────────────────────
$Timestamp  = Get-Date -Format "yyyyMMdd-HHmmss"
$ReportFile = Join-Path $OutputDir "flakiness-report-$Timestamp.json"
$ReportData = @{
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    TrxDirectory   = $TrxDir
    TrxFilesRead   = $TrxFiles.Count
    TotalAnalysed  = $TotalAnalysed
    SloThreshold   = $MaxFlakiness
    SloBreached    = $SloBreached
    FlakyTests     = $FlakyTests
}
$ReportData | ConvertTo-Json -Depth 5 | Out-File $ReportFile -Encoding UTF8
Write-Host ""
Write-Host "Report saved: $ReportFile" -ForegroundColor Cyan

# ── Final SLO verdict ──────────────────────────────────────────────────────────
Write-Host ""
if ($SloBreached) {
    Write-Host "🔴 QA-18 SLO BREACHED — Flakiness rate exceeds $MaxFlakiness% threshold." -ForegroundColor Red
    if ($Fail) { exit 1 }
} else {
    Write-Host "✅ QA-18 SLO MET — All tests within $MaxFlakiness% flakiness threshold." -ForegroundColor Green
}
Write-Host ""

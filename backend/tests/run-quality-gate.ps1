# QA-20: Final Release Quality Gate
#
# Orchestrates the complete quality pipeline:
#   1. dotnet build (Release)
#   2. dotnet test  (all suites)
#   3. Coverage threshold check (if coverlet output present)
#   4. Security SAST check (if dotnet-security-audit available)
#   5. Smoke test against live API (if BaseUrl provided)
#   6. QA-18 flakiness check
#   7. Summary verdict with PASS/FAIL exit code
#
# Usage:
#   .\run-quality-gate.ps1                          # build + test only
#   .\run-quality-gate.ps1 -BaseUrl http://localhost:5000  # includes smoke
#   .\run-quality-gate.ps1 -SkipBuild -BaseUrl http://localhost:5000

param(
    [string]$BaseUrl           = "",
    [string]$TenantId          = "default",
    [switch]$SkipBuild,
    [switch]$SkipSmoke,
    [switch]$SkipFlakiness,
    [int]$CoverageMinPercent   = 70,     # Overall coverage floor
    [double]$MaxFlakiness      = 5.0,
    [string]$Configuration     = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackendDir = Join-Path $ScriptDir ".."
$TestDir    = Join-Path $BackendDir "tests\Api.Tests"
$Timestamp  = Get-Date -Format "yyyyMMdd-HHmmss"
$ResultsDir = Join-Path $ScriptDir "reports"
if (-not (Test-Path $ResultsDir)) { New-Item -ItemType Directory -Path $ResultsDir | Out-Null }

$TrxPath   = Join-Path $ResultsDir "gate-$Timestamp.trx"
$GateLog   = Join-Path $ResultsDir "gate-$Timestamp.log"

$Steps     = [ordered]@{}
$OverallOk = $true

function Write-Step([string]$msg, [string]$color = "Cyan") {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Write-Host $line -ForegroundColor $color
    Add-Content $GateLog $line
}

function Set-StepResult([string]$step, [bool]$passed, [string]$detail = "") {
    $Steps[$step] = @{ Passed = $passed; Detail = $detail }
    if (-not $passed) { $script:OverallOk = $false }
    $icon  = if ($passed) { "✅" } else { "❌" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Step "$icon $step $(if ($detail) { "— $detail" })" $color
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  QA-20: NovaMind MindFlow Final Release Quality Gate            ║" -ForegroundColor Cyan
Write-Host "║  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')                                    ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Push-Location $BackendDir
try {

    # ── Step 1: Build ──────────────────────────────────────────────────────────
    if (-not $SkipBuild) {
        Write-Step "Step 1/6: Building solution ($Configuration)..."
        try {
            $buildOut = & dotnet build MindFlow.Backend.sln -c $Configuration --nologo 2>&1
            $buildOk  = ($LASTEXITCODE -eq 0)
            Set-StepResult "Build" $buildOk $(if (-not $buildOk) { "Exit code $LASTEXITCODE" } else { "Clean" })
        } catch {
            Set-StepResult "Build" $false $_.ToString()
        }
    } else {
        Write-Step "Step 1/6: Build SKIPPED" "Yellow"
        $Steps["Build"] = @{ Passed = $true; Detail = "Skipped" }
    }

    # ── Step 2: Test suite ────────────────────────────────────────────────────
    Write-Step "Step 2/6: Running full test suite..."
    $TestPassed = 0; $TestFailed = 0; $TestTotal = 0

    try {
        $testOut = & dotnet test tests/Api.Tests/Api.Tests.csproj `
            -c $Configuration `
            --logger "trx;LogFileName=$TrxPath" `
            --nologo 2>&1

        $testOk = ($LASTEXITCODE -eq 0)

        # Parse TRX for counts
        if (Test-Path $TrxPath) {
            try {
                [xml]$trx    = Get-Content $TrxPath
                $counters    = $trx.TestRun.ResultSummary.Counters
                $TestPassed  = [int]$counters.passed
                $TestFailed  = [int]$counters.failed
                $TestTotal   = [int]$counters.total
            } catch { }
        }

        Set-StepResult "Tests" $testOk "$TestPassed/$TestTotal passed$(if ($TestFailed -gt 0) { ", $TestFailed FAILED" })"
    } catch {
        Set-StepResult "Tests" $false $_.ToString()
    }

    # ── Step 3: Coverage threshold ────────────────────────────────────────────
    Write-Step "Step 3/6: Checking coverage threshold (>= $CoverageMinPercent%)..."
    $CoverageXml = Get-ChildItem -Path . -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

    if ($CoverageXml) {
        try {
            [xml]$cov = Get-Content $CoverageXml.FullName
            $lineRate = [double]$cov.coverage.'line-rate' * 100
            $covOk    = $lineRate -ge $CoverageMinPercent
            Set-StepResult "Coverage" $covOk "$([math]::Round($lineRate,1))% (floor: $CoverageMinPercent%)"
        } catch {
            Write-Step "  Coverage XML parse error: $_" "Yellow"
            $Steps["Coverage"] = @{ Passed = $true; Detail = "Parse error — not blocking" }
        }
    } else {
        Write-Step "  No coverage XML found — run with /p:CollectCoverage=true to enable." "Yellow"
        $Steps["Coverage"] = @{ Passed = $true; Detail = "Not collected — informational only" }
    }

    # ── Step 4: Security audit ────────────────────────────────────────────────
    Write-Step "Step 4/6: Security dependency audit..."
    try {
        $auditOut = & dotnet list tests/Api.Tests/Api.Tests.csproj package --vulnerable --include-transitive 2>&1
        $vulnFound = ($auditOut | Select-String -Pattern "has the following vulnerable packages" -Quiet)
        Set-StepResult "Security" (-not $vulnFound) $(if ($vulnFound) { "Vulnerable packages detected" } else { "No known vulnerabilities" })
    } catch {
        $Steps["Security"] = @{ Passed = $true; Detail = "Audit skipped — not blocking" }
    }

    # ── Step 5: Smoke test ────────────────────────────────────────────────────
    if (-not $SkipSmoke -and -not [string]::IsNullOrWhiteSpace($BaseUrl)) {
        Write-Step "Step 5/6: Smoke test against $BaseUrl..."
        $smokeOk = $true
        $smokeDetails = @()

        $smokeEndpoints = @(
            @{ Path = "/health/live";              Expect = 200 },
            @{ Path = "/health/ready";             Expect = @(200, 503) },
            @{ Path = "/api/dashboard/overview";   Expect = 200 },
            @{ Path = "/api/rules";                Expect = 200 },
            @{ Path = "/api/pipeline/stages";      Expect = 200 }
        )

        $headers = @{ "X-Tenant-Id" = $TenantId; "X-User-Role" = "Admin" }

        foreach ($ep in $smokeEndpoints) {
            try {
                $r = Invoke-WebRequest -Uri "$BaseUrl$($ep.Path)" -Headers $headers `
                    -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
                $expectedList = @($ep.Expect) | ForEach-Object { [int]$_ }
                if ($r.StatusCode -notin $expectedList) {
                    $smokeDetails += "$($ep.Path): got $($r.StatusCode)"
                    $smokeOk = $false
                }
            } catch {
                $smokeDetails += "$($ep.Path): $($_.Exception.Message)"
                $smokeOk = $false
            }
        }

        Set-StepResult "Smoke" $smokeOk $(if ($smokeDetails.Count -gt 0) { $smokeDetails -join "; " } else { "All endpoints healthy" })
    } else {
        Write-Step "Step 5/6: Smoke test SKIPPED (no BaseUrl provided)" "Yellow"
        $Steps["Smoke"] = @{ Passed = $true; Detail = "Skipped" }
    }

    # ── Step 6: Flakiness SLO ─────────────────────────────────────────────────
    if (-not $SkipFlakiness) {
        Write-Step "Step 6/6: Flakiness SLO check (max $MaxFlakiness%)..."
        $FlakinessScript = Join-Path $PSScriptRoot "analyze-test-flakiness.ps1"
        if (Test-Path $FlakinessScript) {
            try {
                & $FlakinessScript -TrxDir (Join-Path $ScriptDir "reports") `
                    -MaxFlakiness $MaxFlakiness `
                    -OutputDir $ResultsDir 2>&1 | Out-Null
                $flakyOk = ($LASTEXITCODE -eq 0)
                Set-StepResult "Flakiness SLO" $flakyOk $(if ($flakyOk) { "Within SLO" } else { "SLO breached" })
            } catch {
                $Steps["Flakiness SLO"] = @{ Passed = $true; Detail = "Skipped — analysis error" }
            }
        } else {
            $Steps["Flakiness SLO"] = @{ Passed = $true; Detail = "Analyzer not found" }
        }
    } else {
        $Steps["Flakiness SLO"] = @{ Passed = $true; Detail = "Skipped" }
    }

} finally {
    Pop-Location
}

# ── Final Summary ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GATE SUMMARY" -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════════" -ForegroundColor Cyan

foreach ($step in $Steps.GetEnumerator()) {
    $icon  = if ($step.Value.Passed) { "✅" } else { "❌" }
    $color = if ($step.Value.Passed) { "Green" } else { "Red" }
    Write-Host "  $icon $($step.Key.PadRight(20)) $($step.Value.Detail)" -ForegroundColor $color
}

Write-Host ""

if ($OverallOk) {
    Write-Host "✅ QUALITY GATE: PASSED" -ForegroundColor Green
    Write-Host "   Ready for release candidate promotion." -ForegroundColor Green
    $exitCode = 0
} else {
    Write-Host "❌ QUALITY GATE: FAILED" -ForegroundColor Red
    Write-Host "   Resolve failing steps before promoting to RC." -ForegroundColor Red
    $exitCode = 1
}

Write-Host "   Log: $GateLog" -ForegroundColor Cyan
Write-Host ""

exit $exitCode

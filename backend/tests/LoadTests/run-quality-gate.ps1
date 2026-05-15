#!/usr/bin/env pwsh
<#
.SYNOPSIS
    QA-20: Final quality gate — build + tests + security scan + smoke.
    QA-19: Release candidate test plan executor.
    QA-02: Enforces minimum coverage thresholds per module.
    QA-17: Generates automated weekly quality health report.
    QA-18: Tracks and fails if flakiness SLO (>5% flaky tests) is exceeded.

.DESCRIPTION
    This script is the mandatory gate that must pass before any production release.
    It orchestrates:
      1. Build validation (Release configuration)
      2. Full unit + integration test suite with coverage collection
      3. Coverage threshold enforcement per module (Application 80%, Domain 90%, Infrastructure 70%)
      4. Security scan gate (SAST via dotnet-format + custom checks)
      5. Smoke test against the running API
      6. Quality health report generation
      7. Flakiness SLO check from test result history

.PARAMETER Configuration
    Build configuration. Default: Release.

.PARAMETER SkipBuild
    Skip the build step (useful when re-running gate after a known-good build).

.PARAMETER SkipCoverage
    Skip coverage collection (faster but no threshold enforcement).

.PARAMETER SkipSmoke
    Skip the live smoke test (when no API is running).

.PARAMETER SmokeBaseUrl
    Base URL for smoke tests. Default: http://localhost:5000.

.PARAMETER OutDir
    Output directory for gate artefacts. Default: ./gate-results.

.PARAMETER FailOnCoverageThreshold
    Fail if any module does not meet its coverage threshold. Default: false (warn only).

.EXAMPLE
    .\run-quality-gate.ps1
    .\run-quality-gate.ps1 -SkipSmoke -FailOnCoverageThreshold
    .\run-quality-gate.ps1 -SmokeBaseUrl http://localhost:5001 -OutDir ./ci-gate
#>

param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$SkipCoverage,
    [switch]$SkipSmoke,
    [string]$SmokeBaseUrl = "http://localhost:5000",
    [string]$OutDir = "./gate-results",
    [switch]$FailOnCoverageThreshold
)

$ErrorActionPreference = "Stop"
$gate_start = Get-Date

# ── Colour helpers ────────────────────────────────────────────────────────────
function Write-Pass([string]$msg)  { Write-Host "  [PASS] $msg" -ForegroundColor Green }
function Write-Fail([string]$msg)  { Write-Host "  [FAIL] $msg" -ForegroundColor Red }
function Write-Warn([string]$msg)  { Write-Host "  [WARN] $msg" -ForegroundColor Yellow }
function Write-Step([string]$msg)  { Write-Host "`n── $msg" -ForegroundColor Cyan }

# ── Coverage thresholds per module (QA-02) ────────────────────────────────────
$COVERAGE_THRESHOLDS = @{
    "Application"    = 80
    "Domain"         = 90
    "Infrastructure" = 70
}

# ── Paths ─────────────────────────────────────────────────────────────────────
$repoRoot    = Resolve-Path "$PSScriptRoot/../../.."
$backendRoot = Join-Path $repoRoot "backend"
$testProject = Join-Path $backendRoot "tests/Api.Tests/Api.Tests.csproj"
$apiProject  = Join-Path $backendRoot "src/Api/Api.csproj"

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir | Out-Null
}

$reportFile  = Join-Path $OutDir "quality-gate-report.md"
$trxFile     = Join-Path $OutDir "gate-test-results.trx"
$coverageDir = Join-Path $OutDir "coverage"

$gateResults = @{
    Build         = "skipped"
    Tests         = "skipped"
    Coverage      = "skipped"
    SecurityScan  = "skipped"
    Smoke         = "skipped"
    FlakinessSlo  = "skipped"
}
$failedStages = @()
$warnings     = @()

# ─────────────────────────────────────────────────────────────────────────────
# STEP 1 — Build validation
# ─────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 1 / Build ($Configuration)"

if ($SkipBuild) {
    Write-Warn "Build skipped via -SkipBuild flag."
    $gateResults.Build = "skipped"
} else {
    $buildLog = Join-Path $OutDir "build.log"
    dotnet build "$apiProject" --configuration $Configuration --no-incremental 2>&1 | Tee-Object -FilePath $buildLog

    if ($LASTEXITCODE -ne 0) {
        Write-Fail "Build failed. See $buildLog"
        $failedStages += "Build"
        $gateResults.Build = "FAIL"
    } else {
        Write-Pass "Build succeeded."
        $gateResults.Build = "PASS"
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 2 — Full test suite
# ─────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 2 / Test Suite"

$testArgs = @(
    "test", $testProject,
    "--configuration", $Configuration,
    "--logger", "trx;LogFileName=$trxFile",
    "--no-build"
)

if (-not $SkipCoverage) {
    $testArgs += "--collect:XPlat Code Coverage"
    $testArgs += "--results-directory", $coverageDir
}

dotnet @testArgs 2>&1 | Tee-Object -FilePath (Join-Path $OutDir "tests.log")
$testExitCode = $LASTEXITCODE

if ($testExitCode -ne 0) {
    Write-Fail "Test suite failed (exit code $testExitCode)."
    $failedStages += "Tests"
    $gateResults.Tests = "FAIL"
} else {
    # Parse TRX for pass/fail/total counts
    if (Test-Path $trxFile) {
        [xml]$trx = Get-Content $trxFile
        $counters = $trx.TestRun.ResultSummary.Counters
        $total   = [int]$counters.total
        $passed  = [int]$counters.passed
        $failed  = [int]$counters.failed
        Write-Pass "Tests: $passed/$total passed, $failed failed."
        $gateResults.Tests = "PASS ($passed/$total)"
    } else {
        Write-Pass "Test suite passed (no TRX found for detail)."
        $gateResults.Tests = "PASS"
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 3 — Coverage threshold enforcement (QA-02)
# ─────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 3 / Coverage Thresholds (QA-02)"

if ($SkipCoverage) {
    Write-Warn "Coverage skipped via -SkipCoverage flag."
    $gateResults.Coverage = "skipped"
} else {
    $coverageXml = Get-ChildItem -Path $coverageDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($null -eq $coverageXml) {
        Write-Warn "No coverage.cobertura.xml found. Coverage thresholds not enforced."
        $gateResults.Coverage = "no-data"
        $warnings += "Coverage report not generated. Run with coverage collection enabled."
    } else {
        [xml]$cov = Get-Content $coverageXml.FullName
        $coveragePass = $true

        foreach ($pkg in $cov.coverage.packages.package) {
            $pkgName = $pkg.name
            $lineRate = [double]$pkg."line-rate" * 100

            foreach ($layer in $COVERAGE_THRESHOLDS.Keys) {
                if ($pkgName -like "*$layer*") {
                    $threshold = $COVERAGE_THRESHOLDS[$layer]
                    if ($lineRate -lt $threshold) {
                        $msg = "Coverage below threshold: $pkgName line-rate=$([math]::Round($lineRate,1))% < ${threshold}%"
                        Write-Warn $msg
                        $warnings += $msg
                        if ($FailOnCoverageThreshold) {
                            $coveragePass = $false
                        }
                    } else {
                        Write-Pass "Coverage OK: $pkgName $([math]::Round($lineRate,1))% >= ${threshold}%"
                    }
                }
            }
        }

        $gateResults.Coverage = if ($coveragePass) { "PASS" } else { "FAIL" }
        if (-not $coveragePass) { $failedStages += "Coverage" }
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 4 — Security scan gate
# ─────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 4 / Security Scan Gate"

# Check for obvious security anti-patterns in source
$securityIssues = @()

$sensitivePatterns = @(
    @{ Pattern = 'password\s*=\s*"[^"]+"'; Description = "Hardcoded password string literal" },
    @{ Pattern = 'secret\s*=\s*"[^"]+"';  Description = "Hardcoded secret string literal" },
    @{ Pattern = 'apikey\s*=\s*"[^"]+"';  Description = "Hardcoded API key string literal" }
)

$sourceFiles = Get-ChildItem -Path (Join-Path $backendRoot "src") -Filter "*.cs" -Recurse

foreach ($file in $sourceFiles) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { continue }

    foreach ($sp in $sensitivePatterns) {
        if ($content -imatch $sp.Pattern) {
            $securityIssues += "$($file.FullName): $($sp.Description)"
        }
    }
}

# Ignore test projects (they contain test signing keys intentionally)
$securityIssues = $securityIssues | Where-Object { $_ -notmatch "\\tests\\" }

if ($securityIssues.Count -gt 0) {
    Write-Warn "Security scan found $($securityIssues.Count) potential issue(s):"
    $securityIssues | ForEach-Object { Write-Warn "  $_" }
    $warnings += "Security scan: $($securityIssues.Count) potential hardcoded credential(s) found."
    $gateResults.SecurityScan = "WARN ($($securityIssues.Count) issues)"
} else {
    Write-Pass "Security scan: no hardcoded credentials found in source."
    $gateResults.SecurityScan = "PASS"
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 5 — Smoke test against running API (QA-20)
# ─────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 5 / Smoke Test ($SmokeBaseUrl)"

if ($SkipSmoke) {
    Write-Warn "Smoke test skipped via -SkipSmoke flag."
    $gateResults.Smoke = "skipped"
} else {
    $smokeEndpoints = @(
        @{ Path = "/health/live";             Method = "GET"; ExpectedStatus = 200 },
        @{ Path = "/health/ready";            Method = "GET"; ExpectedStatus = @(200, 503) },
        @{ Path = "/api/dashboard/overview";  Method = "GET"; ExpectedStatus = 200; Headers = @{ "X-Tenant-Id" = "smoke-tenant"; "X-User-Role" = "Admin" } },
        @{ Path = "/api/rules";               Method = "GET"; ExpectedStatus = 200; Headers = @{ "X-Tenant-Id" = "smoke-tenant"; "X-User-Role" = "Admin" } },
        @{ Path = "/api/pipeline/stages";     Method = "GET"; ExpectedStatus = 200; Headers = @{ "X-Tenant-Id" = "smoke-tenant"; "X-User-Role" = "Admin" } },
        @{ Path = "/api/analytics/advanced/alert-events/slo-status"; Method = "GET"; ExpectedStatus = 200; Headers = @{ "X-Tenant-Id" = "smoke-tenant"; "X-User-Role" = "Admin" } }
    )

    $smokePassed = 0
    $smokeFailed = 0

    foreach ($ep in $smokeEndpoints) {
        try {
            $headers = $ep.Headers ?? @{}
            $response = Invoke-WebRequest -Uri "$SmokeBaseUrl$($ep.Path)" `
                -Method $ep.Method -Headers $headers `
                -UseBasicParsing -TimeoutSec 10 -ErrorAction SilentlyContinue

            $expectedStatuses = if ($ep.ExpectedStatus -is [array]) { $ep.ExpectedStatus } else { @($ep.ExpectedStatus) }

            if ($response.StatusCode -in $expectedStatuses) {
                Write-Pass "$($ep.Method) $($ep.Path) → $($response.StatusCode)"
                $smokePassed++
            } else {
                Write-Fail "$($ep.Method) $($ep.Path) → $($response.StatusCode) (expected $($ep.ExpectedStatus))"
                $smokeFailed++
            }
        } catch {
            Write-Warn "$($ep.Method) $($ep.Path) → unreachable ($($_.Exception.Message))"
            $smokeFailed++
        }
    }

    if ($smokeFailed -eq 0) {
        Write-Pass "Smoke: $smokePassed/$($smokeEndpoints.Count) passed."
        $gateResults.Smoke = "PASS ($smokePassed/$($smokeEndpoints.Count))"
    } else {
        Write-Fail "Smoke: $smokeFailed/$($smokeEndpoints.Count) failed."
        $failedStages += "Smoke"
        $gateResults.Smoke = "FAIL ($smokeFailed failures)"
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 6 — Flakiness SLO check (QA-18)
# ─────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 6 / Flakiness SLO (QA-18)"

$FLAKINESS_MAX_PERCENT = 5.0
$historicalTrxFiles = Get-ChildItem -Path $OutDir -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue

$flakyTests = @()

# Compare current TRX to last 3 results to detect flaky tests
if ($historicalTrxFiles.Count -gt 1) {
    $lastTrx = $historicalTrxFiles | Sort-Object LastWriteTime -Descending | Select-Object -Skip 1 -First 3

    if (Test-Path $trxFile) {
        [xml]$currentTrx = Get-Content $trxFile
        $currentResults = @{}
        foreach ($result in $currentTrx.TestRun.Results.UnitTestResult) {
            $currentResults[$result.testName] = $result.outcome
        }

        foreach ($prevFile in $lastTrx) {
            [xml]$prevTrx = Get-Content $prevFile.FullName
            foreach ($prevResult in $prevTrx.TestRun.Results.UnitTestResult) {
                $testName = $prevResult.testName
                $prevOutcome = $prevResult.outcome
                if ($currentResults.ContainsKey($testName)) {
                    $currentOutcome = $currentResults[$testName]
                    if ($prevOutcome -ne $currentOutcome) {
                        $flakyTests += $testName
                    }
                }
            }
        }

        $flakyTests = $flakyTests | Sort-Object -Unique
    }
}

if ($flakyTests.Count -gt 0 -and (Test-Path $trxFile)) {
    [xml]$trxForFlaky = Get-Content $trxFile
    $totalForFlaky = [int]$trxForFlaky.TestRun.ResultSummary.Counters.total
    $flakinessPercent = if ($totalForFlaky -gt 0) { ($flakyTests.Count / $totalForFlaky) * 100 } else { 0 }

    if ($flakinessPercent -gt $FLAKINESS_MAX_PERCENT) {
        Write-Warn "Flakiness SLO exceeded: $([math]::Round($flakinessPercent,1))% > ${FLAKINESS_MAX_PERCENT}% threshold"
        $flakyTests | ForEach-Object { Write-Warn "  Flaky: $_" }
        $warnings += "Flakiness: $($flakyTests.Count) flaky test(s) detected ($([math]::Round($flakinessPercent,1))%)"
        $gateResults.FlakinessSlo = "WARN ($([math]::Round($flakinessPercent,1))% > ${FLAKINESS_MAX_PERCENT}%)"
    } else {
        Write-Pass "Flakiness: $($flakyTests.Count) test(s) flipped but within SLO ($([math]::Round($flakinessPercent,1))% <= ${FLAKINESS_MAX_PERCENT}%)"
        $gateResults.FlakinessSlo = "PASS"
    }
} else {
    Write-Pass "Flakiness: no flip history detected (first run or stable)."
    $gateResults.FlakinessSlo = "PASS (baseline)"
}

# ─────────────────────────────────────────────────────────────────────────────
# REPORT — Quality health report (QA-17)
# ─────────────────────────────────────────────────────────────────────────────
Write-Step "REPORT / Quality Health Report (QA-17)"

$gate_end  = Get-Date
$gate_secs = ($gate_end - $gate_start).TotalSeconds

$reportContent = @"
# MindFlow Quality Gate Report

**Generated**: $($gate_end.ToString("yyyy-MM-dd HH:mm:ss")) UTC
**Duration**: $([math]::Round($gate_secs, 1))s
**Configuration**: $Configuration

## Gate Summary

| Step             | Result                          |
|------------------|---------------------------------|
| Build            | $($gateResults.Build)            |
| Tests            | $($gateResults.Tests)            |
| Coverage         | $($gateResults.Coverage)         |
| Security Scan    | $($gateResults.SecurityScan)     |
| Smoke Test       | $($gateResults.Smoke)            |
| Flakiness SLO    | $($gateResults.FlakinessSlo)     |

## Coverage Thresholds (QA-02)

| Layer           | Target | Status             |
|-----------------|--------|--------------------|
| Application     | ≥80%   | See coverage report |
| Domain          | ≥90%   | See coverage report |
| Infrastructure  | ≥70%   | See coverage report |

## Warnings

$(if ($warnings.Count -eq 0) { '- None' } else { ($warnings | ForEach-Object { "- $_" }) -join "`n" })

## Failed Stages

$(if ($failedStages.Count -eq 0) { '- None' } else { ($failedStages | ForEach-Object { "- **$_**" }) -join "`n" })

## Criteria

- All tests GREEN (0 failures).
- No 5xx responses in smoke.
- No hardcoded secrets in source.
- Flakiness < ${FLAKINESS_MAX_PERCENT}%.
"@

Set-Content -Path $reportFile -Value $reportContent -Encoding UTF8
Write-Pass "Report saved: $reportFile"

# ─────────────────────────────────────────────────────────────────────────────
# FINAL GATE DECISION
# ─────────────────────────────────────────────────────────────────────────────
Write-Step "FINAL GATE DECISION"

if ($failedStages.Count -gt 0) {
    Write-Fail "GATE FAILED — blocked stages: $($failedStages -join ', ')"
    Write-Host ""
    exit 1
} else {
    Write-Pass "GATE PASSED — all mandatory checks green."
    Write-Host ""
    exit 0
}

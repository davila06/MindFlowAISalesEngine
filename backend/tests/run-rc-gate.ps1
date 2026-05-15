# QA-19: Release Candidate Gate Orchestrator
#
# Executes the formal RC gate sequence per ia/qa/release-candidate-test-plan.md.
# Gates 1–5 are always Required. Gates 6–7 are Recommended.
#
# Usage:
#   .\run-rc-gate.ps1 -StagingUrl https://staging.mindflow.io
#   .\run-rc-gate.ps1 -StagingUrl https://staging.mindflow.io -SkipLoad
#   .\run-rc-gate.ps1 -LocalOnly    # build + tests only (no smoke/load)

param(
    [string]$StagingUrl    = "",
    [string]$TenantId      = "default",
    [switch]$SkipLoad,
    [switch]$SkipQaReport,
    [switch]$LocalOnly,
    [string]$Configuration = "Release",
    [string]$Tag           = ""    # e.g. "v1.0.0-rc.1"
)

$ErrorActionPreference = "Stop"
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackendDir = Join-Path $ScriptDir ".."
$Timestamp  = Get-Date -Format "yyyyMMdd-HHmmss"
$ReportsDir = Join-Path $ScriptDir "reports"
if (-not (Test-Path $ReportsDir)) { New-Item -ItemType Directory -Path $ReportsDir | Out-Null }
$RcLogPath  = Join-Path $ReportsDir "rc-gate-$Timestamp.log"

$GateResults = [ordered]@{}
$OverallPass = $true

function Write-Gate([string]$msg, [string]$color = "Cyan") {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Write-Host $line -ForegroundColor $color
    Add-Content $RcLogPath $line
}

function Record([string]$gate, [bool]$ok, [string]$detail = "", [bool]$required = $true) {
    $GateResults[$gate] = @{ OK = $ok; Detail = $detail; Required = $required }
    if (-not $ok -and $required) { $script:OverallPass = $false }
    $icon  = if ($ok) { "✅" } else { if ($required) { "❌" } else { "⚠️" } }
    $color = if ($ok) { "Green" } elseif ($required) { "Red" } else { "Yellow" }
    Write-Gate "$icon GATE $gate $(if ($detail) { "— $detail" })" $color
}

# ── Header ────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║  QA-19: NovaMind MindFlow Release Candidate Gate                 ║" -ForegroundColor Magenta
Write-Host "║  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')   $(if($Tag){"Tag: $Tag".PadRight(20)}else{"".PadRight(28)})     ║" -ForegroundColor Magenta
Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

Push-Location $BackendDir
try {

    # ── Gate 1: Build ─────────────────────────────────────────────────────────
    Write-Gate "=== GATE 1: Build & Compile (Required) ==="
    try {
        $out = & dotnet build MindFlow.Backend.sln -c $Configuration --nologo 2>&1
        Record "1-Build" ($LASTEXITCODE -eq 0) $(if ($LASTEXITCODE -eq 0) { "Clean" } else { "Exit $LASTEXITCODE" })
    } catch { Record "1-Build" $false $_.ToString() }

    # ── Gate 2: All tests ─────────────────────────────────────────────────────
    Write-Gate "=== GATE 2: Full Test Suite (Required) ==="
    $TrxPath = Join-Path $ReportsDir "rc-tests-$Timestamp.trx"
    try {
        & dotnet test tests/Api.Tests/Api.Tests.csproj `
            -c $Configuration `
            --logger "trx;LogFileName=$TrxPath" `
            --nologo 2>&1 | Out-Null

        $testsOk = ($LASTEXITCODE -eq 0)
        $passed  = 0; $failed = 0

        if (Test-Path $TrxPath) {
            [xml]$trx  = Get-Content $TrxPath
            $c = $trx.TestRun.ResultSummary.Counters
            $passed = [int]$c.passed; $failed = [int]$c.failed
        }

        Record "2-Tests" $testsOk "$passed passed$(if ($failed -gt 0) { ", $failed FAILED" })"
    } catch { Record "2-Tests" $false $_.ToString() }

    # ── Gate 3: Focused mutation / regression ─────────────────────────────────
    Write-Gate "=== GATE 3: Mutation & Regression Suites (Required) ==="
    $suites = @("QaMutationRulesCriticalTests", "QaConcurrencyPipelineTests")
    $suiteOk = $true; $detail3 = @()
    foreach ($suite in $suites) {
        try {
            & dotnet test tests/Api.Tests/Api.Tests.csproj `
                --filter "FullyQualifiedName~$suite" `
                -c $Configuration --nologo 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) { $suiteOk = $false; $detail3 += "$suite FAILED" }
            else { $detail3 += "$suite OK" }
        } catch { $suiteOk = $false; $detail3 += "$suite error" }
    }
    Record "3-Regression" $suiteOk ($detail3 -join "; ")

    # ── Gate 4: Security ──────────────────────────────────────────────────────
    Write-Gate "=== GATE 4: Security Checks (Required) ==="
    try {
        & dotnet test tests/Api.Tests/Api.Tests.csproj `
            --filter "FullyQualifiedName~SecurityHardening" `
            -c $Configuration --nologo 2>&1 | Out-Null
        $secOk = ($LASTEXITCODE -eq 0)

        # Dependency audit
        $audit = & dotnet list src/Api/Api.csproj package --vulnerable --include-transitive 2>&1
        $vulns = ($audit | Select-String "has the following vulnerable packages" -Quiet)
        if ($vulns) { $secOk = $false }

        Record "4-Security" $secOk $(if ($secOk) { "Security tests green, no vulnerable deps" } else { "Issues detected" })
    } catch { Record "4-Security" $false $_.ToString() }

    # ── Gate 5: Smoke ─────────────────────────────────────────────────────────
    Write-Gate "=== GATE 5: Smoke Tests (Required when StagingUrl provided) ==="
    if (-not $LocalOnly -and -not [string]::IsNullOrWhiteSpace($StagingUrl)) {
        $smokeOk = $true; $smokeIssues = @()
        $headers = @{ "X-Tenant-Id" = $TenantId; "X-User-Role" = "Admin" }
        $endpoints = @("/health/live", "/api/dashboard/overview", "/api/rules", "/api/pipeline/stages")
        foreach ($ep in $endpoints) {
            try {
                $r = Invoke-WebRequest -Uri "$StagingUrl$ep" -Headers $headers `
                    -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop
                if ($r.StatusCode -notin @(200, 201)) {
                    $smokeOk = $false; $smokeIssues += "$ep=$($r.StatusCode)"
                }
            } catch { $smokeOk = $false; $smokeIssues += "$ep=ERROR" }
        }
        Record "5-Smoke" $smokeOk $(if ($smokeIssues.Count -gt 0) { $smokeIssues -join "; " } else { "All endpoints healthy" })
    } else {
        Write-Gate "  Gate 5 skipped (LocalOnly or no StagingUrl)" "Yellow"
        $GateResults["5-Smoke"] = @{ OK = $true; Detail = "Skipped"; Required = $false }
    }

    # ── Gate 6: Load (Recommended) ────────────────────────────────────────────
    Write-Gate "=== GATE 6: Load Tests (Recommended) ==="
    if (-not $SkipLoad -and -not $LocalOnly) {
        $loadScript = Join-Path $ScriptDir "LoadTests\run-intake-analytics-load-tests.ps1"
        if (Test-Path $loadScript) {
            try {
                & $loadScript -Mode smoke -BaseUrl ($StagingUrl ? $StagingUrl : "http://localhost:5000") `
                    -TenantId $TenantId 2>&1 | Out-Null
                Record "6-Load" ($LASTEXITCODE -eq 0) $(if ($LASTEXITCODE -eq 0) { "Smoke passed" } else { "Smoke failed" }) $false
            } catch { Record "6-Load" $false $_.ToString() $false }
        } else {
            $GateResults["6-Load"] = @{ OK = $true; Detail = "Load script not found"; Required = $false }
        }
    } else {
        $GateResults["6-Load"] = @{ OK = $true; Detail = "Skipped"; Required = $false }
    }

    # ── Gate 7: QA Health Report (Recommended) ────────────────────────────────
    Write-Gate "=== GATE 7: QA Health Report (Recommended) ==="
    if (-not $SkipQaReport -and -not [string]::IsNullOrWhiteSpace($StagingUrl)) {
        try {
            $r = Invoke-WebRequest -Uri "$StagingUrl/api/dashboard/qa-health-report" `
                -Headers @{ "X-Tenant-Id" = $TenantId; "X-User-Role" = "Admin" } `
                -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop
            $report    = $r.Content | ConvertFrom-Json
            $gradeOk   = $report.QualityGrade -in @("A", "B")
            Record "7-QaReport" $gradeOk "Grade=$($report.QualityGrade) Score=$($report.QualityScore)" $false
        } catch { Record "7-QaReport" $false $_.ToString() $false }
    } else {
        $GateResults["7-QaReport"] = @{ OK = $true; Detail = "Skipped"; Required = $false }
    }

} finally { Pop-Location }

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "  RC GATE SUMMARY$(if ($Tag) { " — $Tag" })" -ForegroundColor Magenta
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Magenta

foreach ($g in $GateResults.GetEnumerator()) {
    $v     = $g.Value
    $icon  = if ($v.OK) { "✅" } elseif ($v.Required) { "❌" } else { "⚠️" }
    $color = if ($v.OK) { "Green" } elseif ($v.Required) { "Red" } else { "Yellow" }
    $req   = if ($v.Required) { "[Required]" } else { "[Optional]" }
    Write-Host "  $icon $req $($g.Key.PadRight(20)) $($v.Detail)" -ForegroundColor $color
}

Write-Host ""
if ($OverallPass) {
    Write-Host "✅ RC GATE: PASSED — Build may be tagged as Release Candidate." -ForegroundColor Green
    if ($Tag) { Write-Host "   Recommended tag: $Tag" -ForegroundColor Green }
} else {
    Write-Host "❌ RC GATE: FAILED — Required gates did not pass. Do NOT promote to RC." -ForegroundColor Red
}
Write-Host "   Log: $RcLogPath" -ForegroundColor Cyan
Write-Host ""

exit $(if ($OverallPass) { 0 } else { 1 })

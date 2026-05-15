# QA-17: Automated Weekly Quality Health Report Generator
# 
# Runs all tests, collects coverage, fetches live QA health report from API,
# and generates an HTML summary report.
#
# Usage:
#   .\generate-qa-health-report.ps1
#   .\generate-qa-health-report.ps1 -BaseUrl http://localhost:5000 -OutputDir ./reports
#   .\generate-qa-health-report.ps1 -SkipTests

param(
    [string]$BaseUrl   = "http://localhost:5000",
    [string]$TenantId  = "default",
    [string]$OutputDir = "$PSScriptRoot\reports",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }

$ReportPath = Join-Path $OutputDir "qa-health-report-$Timestamp.html"
$BackendDir = Join-Path $PSScriptRoot "..\.."

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  QA-17: Weekly Quality Health Report                        ║" -ForegroundColor Cyan
Write-Host "║  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')                         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Run tests ─────────────────────────────────────────────────────────
$TestResult = "SKIPPED"
$TestPassed = 0
$TestFailed = 0
$TestTotal  = 0

if (-not $SkipTests) {
    Write-Host "[QA-17] Running test suite..." -ForegroundColor Yellow
    $TrxPath = Join-Path $OutputDir "qa-report-$Timestamp.trx"

    Push-Location $BackendDir
    try {
        & dotnet test tests/Api.Tests/Api.Tests.csproj `
            --logger "trx;LogFileName=$TrxPath" `
            --no-build `
            -c Release `
            2>&1 | Tee-Object -Variable TestOutput

        if ($LASTEXITCODE -eq 0) {
            $TestResult = "PASS"
            Write-Host "[QA-17] Tests PASSED" -ForegroundColor Green
        } else {
            $TestResult = "FAIL"
            Write-Host "[QA-17] Tests FAILED" -ForegroundColor Red
        }

        # Parse TRX for counts
        if (Test-Path $TrxPath) {
            [xml]$trx = Get-Content $TrxPath
            $counters = $trx.TestRun.ResultSummary.Counters
            if ($counters) {
                $TestPassed = [int]$counters.passed
                $TestFailed = [int]$counters.failed
                $TestTotal  = [int]$counters.total
            }
        }
    } finally {
        Pop-Location
    }
}

# ── Step 2: Fetch live QA health report ──────────────────────────────────────
Write-Host "[QA-17] Fetching live QA health report from API..." -ForegroundColor Yellow
$QaReport = $null
try {
    $headers = @{
        "X-Tenant-Id"  = $TenantId
        "X-User-Role"  = "Admin"
    }
    $resp = Invoke-RestMethod -Uri "$BaseUrl/api/dashboard/qa-health-report?windowDays=7" `
        -Headers $headers -TimeoutSec 15
    $QaReport = $resp
    Write-Host "[QA-17] API health report fetched. Grade: $($QaReport.qualityGrade), Score: $($QaReport.qualityScore)" -ForegroundColor Green
} catch {
    Write-Host "[QA-17] Could not fetch live API report: $_" -ForegroundColor Yellow
}

# ── Step 3: Generate HTML report ──────────────────────────────────────────────
$Grade      = if ($QaReport) { $QaReport.qualityGrade } else { "N/A" }
$Score      = if ($QaReport) { $QaReport.qualityScore } else { "N/A" }
$TotalLeads = if ($QaReport) { $QaReport.totalLeads }   else { "N/A" }
$Warnings   = if ($QaReport) { ($QaReport.warnings -join "<br/>") } else { "API not available" }
$GradeColor = switch ($Grade) {
    "A" { "#16a34a" }
    "B" { "#65a30d" }
    "C" { "#d97706" }
    "D" { "#ea580c" }
    "F" { "#dc2626" }
    default { "#6b7280" }
}

$Html = @"
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <title>QA-17: Weekly Quality Health Report — $Timestamp</title>
  <style>
    body { font-family: -apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif; background:#f8fafc; margin:0; padding:24px; color:#1e293b; }
    h1   { font-size:1.5rem; font-weight:700; margin-bottom:4px; }
    .subtitle { color:#64748b; font-size:.9rem; margin-bottom:24px; }
    .grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(200px,1fr)); gap:16px; margin-bottom:24px; }
    .card { background:#fff; border:1px solid #e2e8f0; border-radius:8px; padding:16px; }
    .card-label { font-size:.75rem; text-transform:uppercase; letter-spacing:.05em; color:#64748b; margin-bottom:4px; }
    .card-value { font-size:2rem; font-weight:700; }
    .grade-card .card-value { color:$GradeColor; }
    .tests-pass .card-value { color:#16a34a; }
    .tests-fail .card-value { color:#dc2626; }
    .section { background:#fff; border:1px solid #e2e8f0; border-radius:8px; padding:16px; margin-bottom:16px; }
    .section h2 { font-size:1rem; font-weight:600; margin:0 0 12px 0; }
    table { width:100%; border-collapse:collapse; font-size:.875rem; }
    th    { background:#f1f5f9; text-align:left; padding:8px 12px; font-weight:600; }
    td    { padding:8px 12px; border-top:1px solid #e2e8f0; }
    .warn { color:#d97706; }
    .pass { color:#16a34a; font-weight:600; }
    .fail { color:#dc2626; font-weight:600; }
    .badge { display:inline-block; padding:2px 8px; border-radius:999px; font-size:.75rem; font-weight:600; }
    .badge-pass { background:#dcfce7; color:#15803d; }
    .badge-fail { background:#fee2e2; color:#b91c1c; }
    footer { font-size:.75rem; color:#94a3b8; margin-top:24px; }
  </style>
</head>
<body>
  <h1>🧪 NovaMind MindFlow — Weekly QA Health Report</h1>
  <div class="subtitle">Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') UTC &nbsp;|&nbsp; Window: 7 days</div>

  <div class="grid">
    <div class="card grade-card">
      <div class="card-label">Quality Grade</div>
      <div class="card-value">$Grade</div>
    </div>
    <div class="card">
      <div class="card-label">Quality Score</div>
      <div class="card-value">$Score</div>
    </div>
    <div class="card $(if ($TestResult -eq 'PASS') { 'tests-pass' } elseif ($TestResult -eq 'FAIL') { 'tests-fail' } else { '' })">
      <div class="card-label">Test Suite</div>
      <div class="card-value">$TestResult</div>
    </div>
    <div class="card">
      <div class="card-label">Tests Run</div>
      <div class="card-value">$TestTotal</div>
    </div>
  </div>

  <div class="section">
    <h2>📊 Test Results</h2>
    <table>
      <tr><th>Metric</th><th>Value</th><th>Status</th></tr>
      <tr>
        <td>Total Tests</td>
        <td>$TestTotal</td>
        <td>$(if ($TestTotal -gt 0) { '<span class="badge badge-pass">OK</span>' } else { '<span class="badge badge-fail">NO DATA</span>' })</td>
      </tr>
      <tr>
        <td>Passed</td>
        <td>$TestPassed</td>
        <td>$(if ($TestFailed -eq 0 -and $TestTotal -gt 0) { '<span class="badge badge-pass">CLEAN</span>' } else { '<span class="badge badge-fail">HAS FAILURES</span>' })</td>
      </tr>
      <tr>
        <td>Failed</td>
        <td>$TestFailed</td>
        <td>$(if ($TestFailed -eq 0) { '<span class="badge badge-pass">0</span>' } else { "<span class='badge badge-fail'>$TestFailed</span>" })</td>
      </tr>
    </table>
  </div>

  <div class="section">
    <h2>📈 Live API Quality Indicators</h2>
    <table>
      <tr><th>Indicator</th><th>Value</th></tr>
      <tr><td>Total Leads</td><td>$TotalLeads</td></tr>
      <tr><td>New Leads (7d)</td><td>$(if ($QaReport) { $QaReport.newLeadsInWindow } else { 'N/A' })</td></tr>
      <tr><td>Email Completeness</td><td>$(if ($QaReport) { "$($QaReport.leadEmailCompleteness)%" } else { 'N/A' })</td></tr>
      <tr><td>Scoring Coverage</td><td>$(if ($QaReport) { "$($QaReport.scoringCoveragePercent)%" } else { 'N/A' })</td></tr>
      <tr><td>Duplicate Candidates</td><td>$(if ($QaReport) { $QaReport.duplicateCandidateCount } else { 'N/A' })</td></tr>
      <tr><td>Data Anomaly Events</td><td>$(if ($QaReport) { $QaReport.anomalyEventsInWindow } else { 'N/A' })</td></tr>
      <tr><td>Active Opportunities</td><td>$(if ($QaReport) { $QaReport.activeOpportunities } else { 'N/A' })</td></tr>
      <tr><td>Conversion Rate</td><td>$(if ($QaReport) { "$($QaReport.conversionRatePercent)%" } else { 'N/A' })</td></tr>
    </table>
  </div>

  <div class="section">
    <h2>⚠️ Quality Warnings</h2>
    <p>$(if ([string]::IsNullOrWhiteSpace($Warnings)) { '<span class="pass">No warnings — all quality indicators in range.</span>' } else { "<span class='warn'>$Warnings</span>" })</p>
  </div>

  <footer>
    NovaMind MindFlow QA-17 Automated Report &nbsp;|&nbsp; Run: $Timestamp &nbsp;|&nbsp; API: $BaseUrl
  </footer>
</body>
</html>
"@

$Html | Out-File -FilePath $ReportPath -Encoding UTF8
Write-Host ""
Write-Host "[QA-17] HTML report saved: $ReportPath" -ForegroundColor Green
Write-Host ""

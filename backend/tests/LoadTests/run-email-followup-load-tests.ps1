param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$TenantId = "default",
    [string]$OutDir = "./results",
    [switch]$StartApi,
    [string]$ApiProjectPath = "../../src/Api/Api.csproj"
)

$ErrorActionPreference = "Stop"

function Resolve-K6Command {
    $globalK6 = Get-Command k6 -ErrorAction SilentlyContinue
    if ($globalK6) {
        return $globalK6.Source
    }

    $repoRoot = Resolve-Path "$PSScriptRoot/../../.."
    $localK6 = Get-ChildItem -Path (Join-Path $repoRoot ".tools/k6") -Recurse -Filter "k6.exe" -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if ($localK6) {
        return $localK6.FullName
    }

    throw "k6 is not installed (global or local)."
}

function Wait-ApiReady([string]$Base, [string]$Tenant) {
    $healthPath = "/health/live"
    for ($i = 0; $i -lt 120; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "$Base$healthPath" -Headers @{ "X-Tenant-Id" = $Tenant } -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "API is not reachable at $Base$healthPath"
}

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir | Out-Null
}

$k6Command = Resolve-K6Command

$apiProcess = $null
$apiLogOut = Join-Path $OutDir "api-startup-email-followup.log"
$apiLogErr = Join-Path $OutDir "api-startup-email-followup.err.log"
if ($StartApi) {
    $resolvedApiProject = (Resolve-Path $ApiProjectPath).Path
    Write-Host "Starting API from $resolvedApiProject"

    $apiArgs = @("run", "--no-launch-profile", "--urls", $BaseUrl, "--project", "`"$resolvedApiProject`"")
    $apiProcess = Start-Process dotnet -ArgumentList $apiArgs -PassThru -WindowStyle Hidden -RedirectStandardOutput $apiLogOut -RedirectStandardError $apiLogErr
    Wait-ApiReady -Base $BaseUrl -Tenant $TenantId
}
else {
    Wait-ApiReady -Base $BaseUrl -Tenant $TenantId
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$jsonOut = Join-Path $OutDir "email-followup-load-$timestamp.json"
$summaryOut = Join-Path $OutDir "email-followup-load-$timestamp.txt"

$env:BASE_URL = $BaseUrl
$env:TENANT_ID = $TenantId

try {
    & $k6Command run --summary-export "$jsonOut" .\email-followup-load.js | Tee-Object -FilePath $summaryOut
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
    }
}

Write-Host "Email/Follow-up load test completed."
Write-Host "Summary text: $summaryOut"
Write-Host "Summary json: $jsonOut"

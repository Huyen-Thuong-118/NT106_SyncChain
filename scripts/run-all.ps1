# =====================================================================
#  run-all.ps1  -  one command to bring up the whole system
#
#    1) Check database connectivity
#    2) Start the backend in its OWN window (logs stay visible)
#    3) Wait until GET /health returns 200
#    4) Start the Desktop app in THIS window
#
#  Each terminal stays open with colored logs.
# =====================================================================

. "$PSScriptRoot/_common.ps1"

Write-Host ""
Write-Host "====================================" -ForegroundColor Magenta
Write-Host "  SyncChain - full local startup"     -ForegroundColor Magenta
Write-Host "====================================" -ForegroundColor Magenta

# 1) Database check (advisory - never blocks; /health is the real gate).
$db = Get-DatabaseEndpoint
if ($null -eq $db) {
    Write-ErrMsg "No valid DATABASE_URL in .env. Copy .env.example to .env first."
    exit 1
}
Write-Step "Database configured: $($db.DbHost):$($db.Port)"
if (Test-TcpPort -TargetHost $db.DbHost -Port $db.Port -TimeoutMs 5000) {
    Write-Ok "Database reachable"
} else {
    Write-WarnMsg "Database not reachable yet (cloud DB may be slow); continuing."
}

# 2) Start backend in a new window so its logs remain visible.
Write-Step "Starting backend in a new window"
$backendScript = Join-Path $PSScriptRoot "run-backend.ps1"
Start-Process powershell -ArgumentList @(
    "-NoExit", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $backendScript
)

# 3) Wait for readiness.
Write-Step "Waiting for backend /health to return 200 ..."
if (-not (Wait-Backend -MaxWaitSec 120)) {
    Write-ErrMsg "Backend did not become healthy within 120s."
    Write-ErrMsg "Check the backend window for startup/database errors."
    exit 1
}
Write-Ok "Backend healthy at $HealthUrl"

# 4) Run the Desktop app in this window.
Write-Step "Starting Desktop app"
dotnet run --project $DesktopProject -f $DesktopFramework

# =====================================================================
#  run-frontend.ps1
#  Restore + build + run the SyncChain MAUI Desktop app.
#  Warns (does not block) if the backend is not reachable.
# =====================================================================

. "$PSScriptRoot/_common.ps1"

Write-Step "Checking backend availability"
if (Wait-Backend -MaxWaitSec 4 -Quiet) {
    Write-Ok "Backend is up at $ApiBaseUrl"
} else {
    Write-WarnMsg "Backend not detected at $ApiBaseUrl."
    Write-WarnMsg "Start it first with scripts/run-backend.ps1 (or run-all.ps1)."
    Write-WarnMsg "The Login screen will also show a friendly 'server not ready' message."
}

Write-Step "Restoring desktop packages"
dotnet restore $DesktopProject

Write-Step "Building desktop"
dotnet build $DesktopProject -f $DesktopFramework -c Debug --nologo

Write-Step "Running Desktop  (press Ctrl+C to stop)"
dotnet run --project $DesktopProject -f $DesktopFramework

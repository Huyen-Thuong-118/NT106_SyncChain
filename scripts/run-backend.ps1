# =====================================================================
#  run-backend.ps1
#  Restore + build + run the SyncChain API. Streams logs in this window.
# =====================================================================

. "$PSScriptRoot/_common.ps1"

Write-Step "Restoring backend packages"
dotnet restore $ApiProject

Write-Step "Building backend"
dotnet build $ApiProject -c Debug --nologo

Write-Step "Running API at $ApiBaseUrl  (press Ctrl+C to stop)"
Write-Host  "     Health:  $HealthUrl"
Write-Host  "     Swagger: $ApiBaseUrl/swagger"
Write-Host  ""
dotnet run --project $ApiProject

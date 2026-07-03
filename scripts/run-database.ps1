# =====================================================================
#  run-database.ps1
#  Verify the database is reachable using the connection string in .env.
#  Note: this project applies schema + seed automatically when the API
#  starts (EnsureCreated + role/admin seed in Program.cs), so there is no
#  separate "migrate" step to run for local development.
# =====================================================================

. "$PSScriptRoot/_common.ps1"

Write-Step "Checking database configuration (.env)"

$db = Get-DatabaseEndpoint
if ($null -eq $db) {
    Write-ErrMsg "No valid DATABASE_URL found in: $EnvFile"
    Write-Host  "     Copy .env.example to .env and set a PostgreSQL connection string."
    exit 1
}

Write-Host "     DATABASE_URL host : $($db.DbHost)"
Write-Host "     DATABASE_URL port : $($db.Port)"

Write-Step "Testing TCP connectivity to the database"
$ok = $false
for ($attempt = 1; $attempt -le 3; $attempt++) {
    if (Test-TcpPort -TargetHost $db.DbHost -Port $db.Port -TimeoutMs 5000) { $ok = $true; break }
    Write-Host "     attempt $attempt failed, retrying..."
    Start-Sleep -Seconds 1
}

if ($ok) {
    Write-Ok "Database reachable at $($db.DbHost):$($db.Port)"
} else {
    Write-WarnMsg "Could not reach $($db.DbHost):$($db.Port) (a cloud DB may just be slow on first connect)."
    Write-WarnMsg "Continuing anyway - the API startup + /health check is the final judge."
}

Write-Host ""
Write-Host "Schema + seed (roles, admin@gmail.com / 123456) are created automatically" -ForegroundColor DarkGray
Write-Host "by the API on startup. No manual migration command is required." -ForegroundColor DarkGray

# =====================================================================
#  SyncChain dev scripts - shared helpers
#  (ASCII-only on purpose so it renders correctly in any PowerShell host)
# =====================================================================

$ErrorActionPreference = "Stop"
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch {}

# Paths (this file lives in <repo>/scripts)
$script:RepoRoot        = Split-Path $PSScriptRoot -Parent
$script:ApiProject      = Join-Path $RepoRoot "SyncChain.API"
$script:DesktopProject  = Join-Path $RepoRoot "app/SyncChain.Desktop"
$script:EnvFile         = Join-Path $RepoRoot ".env"
$script:ApiPort         = 5292
$script:ApiBaseUrl      = "http://localhost:$ApiPort"
$script:HealthUrl       = "$ApiBaseUrl/health"
$script:DesktopFramework = "net10.0-windows10.0.19041.0"

function Write-Step    { param($m) Write-Host ""; Write-Host "==> $m" -ForegroundColor Cyan }
function Write-Ok      { param($m) Write-Host "[OK]  $m" -ForegroundColor Green }
function Write-WarnMsg { param($m) Write-Host "[!]   $m" -ForegroundColor Yellow }
function Write-ErrMsg  { param($m) Write-Host "[X]   $m" -ForegroundColor Red }

# Check a TCP port (used for the database pre-check).
function Test-TcpPort {
    param([string]$TargetHost, [int]$Port, [int]$TimeoutMs = 5000)
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($TargetHost, $Port, $null, $null)
        if ($async.AsyncWaitHandle.WaitOne($TimeoutMs) -and $client.Connected) {
            $client.EndConnect($async); return $true
        }
        return $false
    } catch { return $false } finally { $client.Close() }
}

# Read the first active DATABASE_URL from .env (same rule as the API's EnvFileLoader).
function Get-DatabaseEndpoint {
    if (-not (Test-Path $EnvFile)) { return $null }
    foreach ($rawLine in Get-Content $EnvFile) {
        $line = $rawLine.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#")) { continue }
        if ($line -notmatch "^DATABASE_URL\s*=") { continue }
        $value = ($line -split "=", 2)[1].Trim().Trim('"').Trim("'")
        try { $uri = [System.Uri]$value } catch { return $null }
        $pgPort = if ($uri.Port -lt 0) { 5432 } else { $uri.Port }
        return [pscustomobject]@{ DbHost = $uri.Host; Port = $pgPort }
    }
    return $null
}

# Poll GET /health until it returns 200 (or timeout). Returns $true/$false.
function Wait-Backend {
    param([int]$MaxWaitSec = 120, [switch]$Quiet)
    $elapsed = 0
    while ($elapsed -lt $MaxWaitSec) {
        try {
            $r = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) { return $true }
        } catch { }
        Start-Sleep -Seconds 2
        $elapsed += 2
        if (-not $Quiet) { Write-Host "  ... waiting for backend ($elapsed s)" }
    }
    return $false
}

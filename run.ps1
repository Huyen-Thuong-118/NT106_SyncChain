# ===========================
# NT106_SyncChain Runner
# ===========================

$ErrorActionPreference = "Stop"

$ApiPort    = 5292
$ApiBaseUrl = "http://localhost:$ApiPort"
$EnvFile    = Join-Path $PSScriptRoot ".env"

# ---------------------------------------------------------------------------
# Helper: kiểm tra một cổng TCP có mở không (dùng cho PostgreSQL).
# ---------------------------------------------------------------------------
function Test-TcpPort {
    param(
        [string]$TargetHost,
        [int]$Port,
        [int]$TimeoutMs = 2000
    )
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($TargetHost, $Port, $null, $null)
        if ($async.AsyncWaitHandle.WaitOne($TimeoutMs) -and $client.Connected) {
            $client.EndConnect($async)
            return $true
        }
        return $false
    } catch {
        return $false
    } finally {
        $client.Close()
    }
}

# ---------------------------------------------------------------------------
# Helper: đọc DATABASE_URL đầu tiên trong .env (đúng như EnvFileLoader của API,
# dòng đầu tiên thắng) rồi tách host/port để kiểm tra.
# ---------------------------------------------------------------------------
function Get-DatabaseEndpoint {
    if (-not (Test-Path $EnvFile)) {
        return $null
    }
    foreach ($rawLine in Get-Content $EnvFile) {
        $line = $rawLine.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#")) { continue }
        if ($line -notmatch "^DATABASE_URL\s*=") { continue }

        $value = ($line -split "=", 2)[1].Trim().Trim('"').Trim("'")
        try {
            $uri = [System.Uri]$value
        } catch {
            return $null
        }
        $pgPort = if ($uri.Port -lt 0) { 5432 } else { $uri.Port }
        return [pscustomobject]@{ DbHost = $uri.Host; Port = $pgPort }
    }
    return $null
}

Write-Host ""
Write-Host "===================================="
Write-Host " Starting SyncChain..."
Write-Host "===================================="
Write-Host ""

# ---------------------------------------------------------------------------
# 1) Kiểm tra PostgreSQL trước khi khởi động API.
#    API sẽ tự tắt nếu không kết nối được DB, nên chặn sớm ở đây cho rõ lỗi.
# ---------------------------------------------------------------------------
$dbEndpoint = Get-DatabaseEndpoint
if ($null -eq $dbEndpoint) {
    Write-Warning "Không đọc được DATABASE_URL hợp lệ trong '$EnvFile'."
    Write-Warning "Hãy sao chép .env.example thành .env và điền chuỗi kết nối PostgreSQL."
    exit 1
}

Write-Host "Checking PostgreSQL at $($dbEndpoint.DbHost):$($dbEndpoint.Port) ..."
if (-not (Test-TcpPort -TargetHost $dbEndpoint.DbHost -Port $dbEndpoint.Port)) {
    Write-Warning "Không kết nối được PostgreSQL tại $($dbEndpoint.DbHost):$($dbEndpoint.Port)."
    Write-Warning "Hãy chắc chắn:"
    Write-Warning "  - Dịch vụ PostgreSQL đang chạy."
    Write-Warning "  - Database trong DATABASE_URL đã được tạo (API chỉ tạo bảng, không tạo database)."
    Write-Warning "  - Host/port/tài khoản trong .env đúng (dòng DATABASE_URL đầu tiên được ưu tiên)."
    exit 1
}
Write-Host "PostgreSQL OK." -ForegroundColor Green

# ---------------------------------------------------------------------------
# 2) Chạy API ở cửa sổ riêng (-NoExit để giữ log lại khi có lỗi).
# ---------------------------------------------------------------------------
Write-Host "Starting API..."
Start-Process powershell `
    -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot'; dotnet run --project SyncChain.API"

# ---------------------------------------------------------------------------
# 3) Chờ API thực sự sẵn sàng (probe Swagger JSON trả 200) trước khi mở Desktop.
#    Kestrel chỉ mở cổng sau khi EnsureCreated() + seed dữ liệu chạy xong, nên
#    khi endpoint này phản hồi là toàn bộ khởi tạo đã hoàn tất.
# ---------------------------------------------------------------------------
$HealthUrl   = "$ApiBaseUrl/swagger/v1/swagger.json"
$MaxWaitSec  = 120
$elapsed     = 0
$apiReady    = $false

Write-Host "Waiting for API to be ready at $ApiBaseUrl ..."
while ($elapsed -lt $MaxWaitSec) {
    try {
        $response = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $apiReady = $true
            break
        }
    } catch {
        # API chưa lên (connection refused / build chưa xong) → chờ tiếp.
    }
    Start-Sleep -Seconds 2
    $elapsed += 2
    Write-Host "  ... $elapsed s"
}

if (-not $apiReady) {
    Write-Warning "API chưa sẵn sàng sau $MaxWaitSec giây. Kiểm tra cửa sổ API để xem lỗi khởi động."
    exit 1
}
Write-Host "API is ready." -ForegroundColor Green

# ---------------------------------------------------------------------------
# 4) Chạy Desktop ở cửa sổ hiện tại.
# ---------------------------------------------------------------------------
Write-Host "Starting Desktop..."
dotnet run --project app/SyncChain.Desktop
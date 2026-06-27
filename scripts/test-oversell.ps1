<#
.SYNOPSIS
Runs concurrent order requests against a dedicated SyncChain test database.

.EXAMPLE
$env:API_BASE_URL = 'http://localhost:5292'
$env:TEST_EMAIL = 'admin@example.test'
$env:TEST_PASSWORD = 'test-password'
$env:TEST_PRODUCT_ID = '1'
$env:CONCURRENT_REQUESTS = '20'
$env:INITIAL_STOCK = '10'
.\scripts\test-oversell.ps1 -ConfirmTestDatabase

The script restores the original product stock and status unless
-KeepFinalStock is supplied. Test orders and their ledger entries remain in
the test database, so never run this script against production.
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl,
    [string]$Token,
    [string]$TestEmail,
    [string]$TestPassword,
    [int]$TestProductId,
    [int]$ConcurrentRequests,
    [int]$InitialStock,
    [switch]$ConfirmTestDatabase,
    [switch]$KeepFinalStock
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

function Get-Setting {
    param([string]$Value, [string]$EnvironmentName, [string]$DefaultValue)

    if (-not [string]::IsNullOrWhiteSpace($Value)) { return $Value }
    $environmentValue = [Environment]::GetEnvironmentVariable($EnvironmentName)
    if (-not [string]::IsNullOrWhiteSpace($environmentValue)) { return $environmentValue }
    return $DefaultValue
}

function Invoke-JsonApi {
    param(
        [string]$Method,
        [string]$Uri,
        [object]$Body,
        [string]$BearerToken
    )

    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
        $headers.Authorization = "Bearer $BearerToken"
    }

    $parameters = @{
        Method = $Method
        Uri = $Uri
        Headers = $headers
        ContentType = 'application/json'
    }
    if ($null -ne $Body) {
        $parameters.Body = $Body | ConvertTo-Json -Depth 10 -Compress
    }

    return Invoke-RestMethod @parameters
}

function Assert-Test {
    param([bool]$Condition, [string]$Message)

    if ($Condition) {
        Write-Host "PASS: $Message" -ForegroundColor Green
    }
    else {
        Write-Host "FAIL: $Message" -ForegroundColor Red
        $script:Failures.Add($Message)
    }
}

function Set-ProductStock {
    param(
        [int]$ProductId,
        [int]$TargetStock,
        [string]$Reason
    )

    $current = Invoke-JsonApi -Method Get `
        -Uri "$script:ApiBaseUrl/api/inventory/products/$ProductId" `
        -Body $null -BearerToken $script:Token
    $difference = $TargetStock - [int]$current.soLuongTon

    if ($difference -ne 0) {
        Invoke-JsonApi -Method Post `
            -Uri "$script:ApiBaseUrl/api/inventory/adjustments" `
            -BearerToken $script:Token `
            -Body @{
                maSanPham = $ProductId
                soLuongThayDoi = $difference
                lyDo = $Reason
                ghiChu = 'scripts/test-oversell.ps1'
            } | Out-Null
    }
}

function Set-ProductStatus {
    param([int]$ProductId, [string]$Status)

    $encodedStatus = [Uri]::EscapeDataString($Status)
    Invoke-JsonApi -Method Put `
        -Uri "$script:ApiBaseUrl/api/Product/$ProductId/status?status=$encodedStatus" `
        -Body $null -BearerToken $script:Token | Out-Null
}

$ApiBaseUrl = Get-Setting $ApiBaseUrl 'API_BASE_URL' 'http://localhost:5292'
$TestEmail = Get-Setting $TestEmail 'TEST_EMAIL' ''
$TestPassword = Get-Setting $TestPassword 'TEST_PASSWORD' ''

if ($TestProductId -le 0) {
    $configuredProductId = Get-Setting '' 'TEST_PRODUCT_ID' ''
    if (-not [int]::TryParse($configuredProductId, [ref]$TestProductId)) {
        throw 'TEST_PRODUCT_ID hoac -TestProductId la bat buoc.'
    }
}

if ($ConcurrentRequests -le 0) {
    $configuredRequests = Get-Setting '' 'CONCURRENT_REQUESTS' '20'
    $ConcurrentRequests = [int]$configuredRequests
}

if ($InitialStock -le 0) {
    $configuredStock = Get-Setting '' 'INITIAL_STOCK' '10'
    $InitialStock = [int]$configuredStock
}

$allowFromEnvironment = [Environment]::GetEnvironmentVariable('ALLOW_INVENTORY_CONCURRENCY_TEST')
if (-not $ConfirmTestDatabase -and $allowFromEnvironment -ne 'true') {
    throw 'Tu choi chay: dung -ConfirmTestDatabase hoac ALLOW_INVENTORY_CONCURRENCY_TEST=true tren database test.'
}

if ($ConcurrentRequests -lt $InitialStock) {
    throw 'CONCURRENT_REQUESTS phai lon hon hoac bang INITIAL_STOCK.'
}

$ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')

if ([string]::IsNullOrWhiteSpace($Token)) {
    if ([string]::IsNullOrWhiteSpace($TestEmail) -or
        [string]::IsNullOrWhiteSpace($TestPassword)) {
        throw 'Can -Token hoac TEST_EMAIL va TEST_PASSWORD.'
    }

    $login = Invoke-JsonApi -Method Post -Uri "$ApiBaseUrl/api/Auth/login" `
        -Body @{ email = $TestEmail; password = $TestPassword } -BearerToken ''
    $Token = [string]$login.token
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'Dang nhap thanh cong nhung response khong co token.'
}

$script:ApiBaseUrl = $ApiBaseUrl
$script:Token = $Token
$script:Failures = [System.Collections.Generic.List[string]]::new()

$original = Invoke-JsonApi -Method Get `
    -Uri "$ApiBaseUrl/api/inventory/products/$TestProductId" `
    -Body $null -BearerToken $Token
$originalStock = [int]$original.soLuongTon
$originalStatus = [string]$original.trangThai

$transactionType = [Uri]::EscapeDataString('Xuat kho don hang')
$historyUri = "$ApiBaseUrl/api/inventory/transactions?productId=$TestProductId&transactionType=$transactionType"
$beforeHistory = @(Invoke-JsonApi -Method Get -Uri $historyUri -Body $null -BearerToken $Token)
$beforeIds = [System.Collections.Generic.HashSet[int]]::new()
foreach ($entry in $beforeHistory) { [void]$beforeIds.Add([int]$entry.maGiaoDich) }

try {
    Set-ProductStock -ProductId $TestProductId -TargetStock $InitialStock `
        -Reason "Dat ton ban dau cho oversell test $(Get-Date -Format o)"
    Set-ProductStatus -ProductId $TestProductId -Status 'Hoat dong'

    $client = [System.Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.Authorization =
        [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $Token)

    $runId = [Guid]::NewGuid().ToString('N')
    $tasks = [System.Collections.Generic.List[System.Threading.Tasks.Task[System.Net.Http.HttpResponseMessage]]]::new()
    $keys = [System.Collections.Generic.List[string]]::new()

    for ($index = 0; $index -lt $ConcurrentRequests; $index++) {
        $key = "oversell-$runId-$index"
        $keys.Add($key)

        $payload = @{
            items = @(@{ maSanPham = $TestProductId; soLuong = 1 })
            idempotencyKey = $key
        } | ConvertTo-Json -Depth 5 -Compress

        $request = [System.Net.Http.HttpRequestMessage]::new(
            [System.Net.Http.HttpMethod]::Post,
            "$ApiBaseUrl/api/Order")
        $request.Headers.Add('Idempotency-Key', $key)
        $request.Content = [System.Net.Http.StringContent]::new(
            $payload,
            [Text.Encoding]::UTF8,
            'application/json')
        $tasks.Add($client.SendAsync($request))
    }

    [System.Threading.Tasks.Task]::WaitAll(
        [System.Threading.Tasks.Task[]]$tasks.ToArray())

    $results = @()
    for ($index = 0; $index -lt $tasks.Count; $index++) {
        $response = $tasks[$index].Result
        $body = $response.Content.ReadAsStringAsync().Result
        $json = $null
        if (-not [string]::IsNullOrWhiteSpace($body)) {
            try { $json = $body | ConvertFrom-Json } catch { }
        }

        $results += [pscustomobject]@{
            Key = $keys[$index]
            StatusCode = [int]$response.StatusCode
            Success = $response.IsSuccessStatusCode
            Body = $body
            Json = $json
        }
    }

    $successful = @($results | Where-Object Success)
    $rejected = @($results | Where-Object { -not $_.Success })
    $serverErrors = @($results | Where-Object { $_.StatusCode -ge 500 })
    $successfulOrderIds = @($successful | ForEach-Object { [int]$_.Json.maDonHang })
    $uniqueOrderIds = @($successfulOrderIds | Sort-Object -Unique)

    $finalStock = Invoke-JsonApi -Method Get `
        -Uri "$ApiBaseUrl/api/inventory/products/$TestProductId" `
        -Body $null -BearerToken $Token
    $afterHistory = @(Invoke-JsonApi -Method Get -Uri $historyUri -Body $null -BearerToken $Token)
    $newHistory = @($afterHistory | Where-Object { -not $beforeIds.Contains([int]$_.maGiaoDich) })
    $movementTotal = ($newHistory | Measure-Object -Property soLuong -Sum).Sum
    if ($null -eq $movementTotal) { $movementTotal = 0 }
    $invalidLedger = @($newHistory | Where-Object {
        [int]$_.tonSau -ne ([int]$_.tonTruoc + [int]$_.soLuong)
    })

    Assert-Test ($successful.Count -eq $InitialStock) `
        "Chinh xac $InitialStock request thanh cong"
    Assert-Test ($rejected.Count -eq ($ConcurrentRequests - $InitialStock)) `
        'So request bi tu choi dung nhu ky vong'
    Assert-Test ($serverErrors.Count -eq 0) 'Khong co response 5xx'
    Assert-Test ([int]$finalStock.soLuongTon -eq 0) 'Ton cuoi bang 0'
    Assert-Test ([int]$finalStock.soLuongTon -ge 0) 'Ton kho khong am'
    Assert-Test ($uniqueOrderIds.Count -eq $InitialStock) `
        "Chi co $InitialStock ma don hang duy nhat"
    Assert-Test ($newHistory.Count -eq $InitialStock) `
        "Chi co $InitialStock giao dich xuat kho moi"
    Assert-Test ([int]$movementTotal -eq -$InitialStock) `
        "Tong xuat kho bang -$InitialStock"
    Assert-Test ($invalidLedger.Count -eq 0) `
        'Moi giao dich thoa TonSau = TonTruoc + SoLuong'

    if ($successful.Count -gt 0) {
        $replayTarget = $successful[0]
        $replayPayload = @{
            items = @(@{ maSanPham = $TestProductId; soLuong = 1 })
            idempotencyKey = $replayTarget.Key
        }
        $replay = Invoke-JsonApi -Method Post -Uri "$ApiBaseUrl/api/Order" `
            -Body $replayPayload -BearerToken $Token
        $historyAfterReplay = @(Invoke-JsonApi -Method Get `
            -Uri $historyUri -Body $null -BearerToken $Token)

        Assert-Test ([bool]$replay.isReplay) 'Retry cung idempotency key duoc danh dau replay'
        Assert-Test ([int]$replay.maDonHang -eq [int]$replayTarget.Json.maDonHang) `
            'Retry tra lai dung don hang cu'
        Assert-Test ($historyAfterReplay.Count -eq $afterHistory.Count) `
            'Retry khong ghi them giao dich kho'
    }
}
finally {
    if (-not $KeepFinalStock) {
        try {
            Set-ProductStock -ProductId $TestProductId -TargetStock $originalStock `
                -Reason "Khoi phuc ton sau oversell test $(Get-Date -Format o)"
            Set-ProductStatus -ProductId $TestProductId -Status $originalStatus
            Write-Host "Da khoi phuc ton=$originalStock, trang thai=$originalStatus" -ForegroundColor Yellow
        }
        catch {
            Write-Warning "Khong the khoi phuc san pham test: $($_.Exception.Message)"
            $script:Failures.Add('Khong the khoi phuc du lieu test')
        }
    }
}

if ($script:Failures.Count -gt 0) {
    throw "Oversell test that bai: $($script:Failures -join '; ')"
}

Write-Host 'Oversell concurrency test passed.' -ForegroundColor Green

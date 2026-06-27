<#
.SYNOPSIS
Integration tests for SyncChain shipping management. Run only against a test database.

.EXAMPLE
./scripts/test-shipping.ps1 -ConfirmTestDatabase -TestProductId 1 `
  -ManagerEmail manager@example.test -ManagerPassword test-password `
  -CustomerEmail customer@example.test -CustomerPassword test-password
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:5292',
    [string]$ManagerToken,
    [string]$AdminToken,
    [string]$CustomerToken,
    [string]$ManagerEmail,
    [string]$ManagerPassword,
    [string]$AdminEmail,
    [string]$AdminPassword,
    [string]$CustomerEmail,
    [string]$CustomerPassword,
    [int]$TestProductId,
    [switch]$ConfirmTestDatabase
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

function Send-Json {
    param($Client, [string]$Method, [string]$Uri, $Body)
    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method), $Uri)
    if ($null -ne $Body) {
        $request.Content = [System.Net.Http.StringContent]::new(
            ($Body | ConvertTo-Json -Depth 10 -Compress),
            [Text.Encoding]::UTF8,
            'application/json')
    }
    $response = $Client.SendAsync($request).Result
    $content = $response.Content.ReadAsStringAsync().Result
    return [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        Success = $response.IsSuccessStatusCode
        Json = $(if ($content) { try { $content | ConvertFrom-Json } catch { $null } })
        Body = $content
    }
}

function Login([string]$Email, [string]$Password) {
    $client = [System.Net.Http.HttpClient]::new()
    $response = Send-Json $client POST "$script:Base/api/Auth/login" @{
        email = $Email; password = $Password
    }
    if (-not $response.Success) { throw "Login failed for ${Email}: $($response.Body)" }
    return [string]$response.Json.token
}

function New-Client([string]$Token) {
    $client = [System.Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.Authorization =
        [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $Token)
    return $client
}

function Assert-True([bool]$Condition, [string]$Message) {
    if ($Condition) { Write-Host "PASS: $Message" -ForegroundColor Green }
    else { Write-Host "FAIL: $Message" -ForegroundColor Red; $script:Failures.Add($Message) }
}

function Assert-Error($Response, [int]$Status, [string]$Code, [string]$Scenario) {
    Assert-True ($Response.StatusCode -eq $Status) "$Scenario HTTP $Status"
    Assert-True ($Response.Json.code -eq $Code) "$Scenario code $Code"
    Assert-True (-not [string]::IsNullOrWhiteSpace($Response.Json.traceId)) "$Scenario co traceId"
}

function Create-Order($Client, [string]$Key) {
    return Send-Json $Client POST "$script:Base/api/Order" @{
        idempotencyKey = $Key
        items = @(@{ maSanPham = $TestProductId; soLuong = 1 })
    }
}

function Create-Shipping([int]$OrderId, [string]$Tracking) {
    return Send-Json $script:Manager POST "$script:Base/api/orders/$OrderId/shipping" @{
        carrier = 'GHN'
        trackingNumber = $Tracking
        shippingFee = 30000
        estimatedDeliveryAt = [DateTime]::UtcNow.AddDays(2).ToString('o')
    }
}

function Update-Shipping([int]$OrderId, [string]$Tracking, [int]$Version) {
    return Send-Json $script:Manager PUT "$script:Base/api/orders/$OrderId/shipping" @{
        carrier = 'GHTK'
        trackingNumber = $Tracking
        shippingFee = 35000
        estimatedDeliveryAt = [DateTime]::UtcNow.AddDays(3).ToString('o')
        concurrencyVersion = $Version
    }
}

function Set-ShippingStatus(
    [int]$OrderId,
    [string]$Status,
    [string]$Expected,
    [int]$Version,
    $Client = $script:Manager) {
    return Send-Json $Client PUT "$script:Base/api/orders/$OrderId/shipping/status" @{
        status = $Status
        expectedStatus = $Expected
        concurrencyVersion = $Version
        note = "shipping integration $script:RunId"
    }
}

function Get-Stock {
    return Send-Json $script:Manager GET "$script:Base/api/inventory/products/$TestProductId" $null
}

function Set-Stock([int]$Target) {
    $current = Get-Stock
    $difference = $Target - [int]$current.Json.soLuongTon
    if ($difference -ne 0) {
        $response = Send-Json $script:Manager POST "$script:Base/api/inventory/adjustments" @{
            maSanPham = $TestProductId
            soLuongThayDoi = $difference
            lyDo = "shipping test $script:RunId"
        }
        if (-not $response.Success) { throw "Cannot set test stock: $($response.Body)" }
    }
}

if (-not $ConfirmTestDatabase -and $env:ALLOW_SHIPPING_TEST -ne 'true') {
    throw 'Refusing to run without -ConfirmTestDatabase or ALLOW_SHIPPING_TEST=true.'
}
$script:Base = $ApiBaseUrl.TrimEnd('/')
if ($TestProductId -le 0) { throw 'TestProductId is required.' }
if (-not $ManagerToken) { $ManagerToken = Login $ManagerEmail $ManagerPassword }
if (-not $AdminToken) { $AdminToken = Login $AdminEmail $AdminPassword }
if (-not $CustomerToken) { $CustomerToken = Login $CustomerEmail $CustomerPassword }
$script:Manager = New-Client $ManagerToken
$script:Admin = New-Client $AdminToken
$script:Customer = New-Client $CustomerToken
$script:Failures = [System.Collections.Generic.List[string]]::new()
$script:RunId = [Guid]::NewGuid().ToString('N')
$originalStock = [int](Get-Stock).Json.soLuongTon

try {
    Set-Stock 20

    $customerOrder = Create-Order $script:Customer "shipping-customer-$script:RunId"
    Assert-True $customerOrder.Success 'Customer tao don test'
    $orderId = [int]$customerOrder.Json.maDonHang
    $tracking = "SHIP-A-$script:RunId"
    $created = Create-Shipping $orderId $tracking
    Assert-True ($created.Success -and $created.Json.shippingStatus -eq 'pending' -and
        [int]$created.Json.concurrencyVersion -eq 0) 'Tao van chuyen pending'
    $shippingId = [int]$created.Json.shippingId

    $duplicate = Create-Shipping $orderId "SHIP-DUP-$script:RunId"
    Assert-Error $duplicate 409 'SHIPPING_ALREADY_EXISTS' 'Trung van chuyen theo don'

    $updated = Update-Shipping $orderId "SHIP-U-$script:RunId" 0
    Assert-True ($updated.Success -and $updated.Json.carrier -eq 'GHTK' -and
        [decimal]$updated.Json.shippingFee -eq 35000 -and
        [int]$updated.Json.concurrencyVersion -eq 1) 'Cap nhat thong tin van chuyen'

    $customerView = Send-Json $script:Customer GET "$script:Base/api/orders/$orderId/shipping" $null
    Assert-True $customerView.Success 'Customer xem van chuyen don cua minh'
    $customerTracking = Send-Json $script:Customer GET `
        "$script:Base/api/shipping/tracking/SHIP-U-$script:RunId" $null
    Assert-True $customerTracking.Success 'Customer tra cuu ma van don cua minh'
    $customerUpdate = Set-ShippingStatus $orderId 'ready' 'pending' 1 $script:Customer
    Assert-True ($customerUpdate.StatusCode -eq 403) 'Customer khong duoc cap nhat van chuyen'

    $ready = Set-ShippingStatus $orderId 'ready' 'pending' 1
    $picked = Set-ShippingStatus $orderId 'picked_up' 'ready' 2
    $transit = Set-ShippingStatus $orderId 'in_transit' 'picked_up' 3
    $delivered = Set-ShippingStatus $orderId 'delivered' 'in_transit' 4
    Assert-True ($ready.Success -and $picked.Success -and $transit.Success -and $delivered.Success) `
        'Luồng pending-ready-picked_up-in_transit-delivered thanh cong'

    $orders = Send-Json $script:Manager GET "$script:Base/api/Order" $null
    $completedOrder = @($orders.Json | Where-Object { [int]$_.maDonHang -eq $orderId })[0]
    Assert-True ($completedOrder.trangThai -eq 'done') 'delivered dong bo don sang done'

    $terminal = Set-ShippingStatus $orderId 'failed' 'delivered' 5
    Assert-Error $terminal 409 'SHIPPING_ALREADY_COMPLETED' 'Cap nhat shipping delivered'
    $history = Send-Json $script:Manager GET "$script:Base/api/orders/$orderId/shipping/history" $null
    Assert-True ($history.Success -and @($history.Json).Count -eq 4) 'Moi transition ghi mot lich su'
    $audits = Send-Json $script:Admin GET `
        "$script:Base/api/audit-logs?entityType=VanChuyen&entityId=$shippingId&pageSize=20" $null
    Assert-True ([int]$audits.Json.totalItems -eq 6) 'Create, update va bon status ghi audit'

    $conflictOrder = Create-Order $script:Manager "shipping-tracking-conflict-$script:RunId"
    $trackingConflict = Create-Shipping ([int]$conflictOrder.Json.maDonHang) "SHIP-U-$script:RunId"
    Assert-Error $trackingConflict 409 'TRACKING_NUMBER_CONFLICT' 'Trung ma van don'

    $invalidOrder = Create-Order $script:Manager "shipping-invalid-$script:RunId"
    $invalidId = [int]$invalidOrder.Json.maDonHang
    $invalidShipping = Create-Shipping $invalidId "SHIP-I-$script:RunId"
    $invalidShippingId = [int]$invalidShipping.Json.shippingId
    $invalid = Set-ShippingStatus $invalidId 'in_transit' 'pending' 0
    Assert-Error $invalid 409 'INVALID_SHIPPING_STATE' 'Bo qua trang thai'
    $invalidHistory = Send-Json $script:Manager GET "$script:Base/api/orders/$invalidId/shipping/history" $null
    $invalidAudits = Send-Json $script:Admin GET `
        "$script:Base/api/audit-logs?entityType=VanChuyen&entityId=$invalidShippingId&pageSize=20" $null
    Assert-True (@($invalidHistory.Json).Count -eq 0) 'Atomic failure khong ghi lich su'
    Assert-True ([int]$invalidAudits.Json.totalItems -eq 1) 'Atomic failure khong ghi audit status'

    $wrongVersion = Set-ShippingStatus $invalidId 'ready' 'pending' 99
    Assert-Error $wrongVersion 409 'CONCURRENCY_CONFLICT' 'Sai shipping version'

    $payload = @{ status='ready'; expectedStatus='pending'; concurrencyVersion=0; note='race' } |
        ConvertTo-Json -Compress
    $uri = "$script:Base/api/orders/$invalidId/shipping/status"
    $request1 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Put, $uri)
    $request2 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Put, $uri)
    foreach ($request in @($request1, $request2)) {
        $request.Content = [System.Net.Http.StringContent]::new($payload, [Text.Encoding]::UTF8, 'application/json')
    }
    $task1 = $script:Manager.SendAsync($request1)
    $task2 = $script:Manager.SendAsync($request2)
    [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($task1, $task2))
    $race = @($task1.Result, $task2.Result) | ForEach-Object {
        $body = $_.Content.ReadAsStringAsync().Result
        [pscustomobject]@{ StatusCode=[int]$_.StatusCode; Success=$_.IsSuccessStatusCode;
            Json=$(try { $body | ConvertFrom-Json } catch { $null }) }
    }
    Assert-True (@($race | Where-Object Success).Count -eq 1) 'Hai request cung version chi mot thanh cong'
    $raceFailure = @($race | Where-Object { -not $_.Success })[0]
    Assert-True ($raceFailure.StatusCode -eq 409 -and $raceFailure.Json.code -eq 'CONCURRENCY_CONFLICT') `
        'Request race con lai nhan CONCURRENCY_CONFLICT'

    $otherCustomerView = Send-Json $script:Customer GET "$script:Base/api/orders/$invalidId/shipping" $null
    Assert-True ($otherCustomerView.StatusCode -eq 403) 'Customer khong xem duoc shipping don khac'

    $cancelOrder = Create-Order $script:Manager "shipping-cancel-$script:RunId"
    $cancelId = [int]$cancelOrder.Json.maDonHang
    $null = Create-Shipping $cancelId "SHIP-C-$script:RunId"
    $cancelled = Set-ShippingStatus $cancelId 'cancelled' 'pending' 0
    Assert-True $cancelled.Success 'pending-cancelled thanh cong'
    $cancelledAgain = Set-ShippingStatus $cancelId 'ready' 'cancelled' 1
    Assert-Error $cancelledAgain 409 'SHIPPING_ALREADY_COMPLETED' 'Cap nhat shipping cancelled'

    $returnedOrder = Create-Order $script:Manager "shipping-returned-$script:RunId"
    $returnedId = [int]$returnedOrder.Json.maDonHang
    $null = Create-Shipping $returnedId "SHIP-R-$script:RunId"
    $null = Set-ShippingStatus $returnedId 'ready' 'pending' 0
    $null = Set-ShippingStatus $returnedId 'picked_up' 'ready' 1
    $failed = Set-ShippingStatus $returnedId 'failed' 'picked_up' 2
    $returned = Set-ShippingStatus $returnedId 'returned' 'failed' 3
    Assert-True ($failed.Success -and $returned.Success) 'picked_up-failed-returned thanh cong'
    $returnedAgain = Set-ShippingStatus $returnedId 'in_transit' 'returned' 4
    Assert-Error $returnedAgain 409 'SHIPPING_ALREADY_COMPLETED' 'Cap nhat shipping returned'

    $retryOrder = Create-Order $script:Manager "shipping-retry-$script:RunId"
    $retryId = [int]$retryOrder.Json.maDonHang
    $null = Create-Shipping $retryId "SHIP-F-$script:RunId"
    $null = Set-ShippingStatus $retryId 'ready' 'pending' 0
    $null = Set-ShippingStatus $retryId 'picked_up' 'ready' 1
    $null = Set-ShippingStatus $retryId 'failed' 'picked_up' 2
    $retryTransit = Set-ShippingStatus $retryId 'in_transit' 'failed' 3
    Assert-True $retryTransit.Success 'failed-in_transit thanh cong'
}
finally {
    try { Set-Stock $originalStock } catch { Write-Warning "Cannot restore stock: $_" }
}

if ($script:Failures.Count -gt 0) {
    throw "Shipping tests failed: $($script:Failures -join '; ')"
}
Write-Host 'Shipping integration tests passed.' -ForegroundColor Green

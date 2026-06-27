<#
.SYNOPSIS
Tests SyncChain inventory and order conflict responses on a dedicated test DB.

.EXAMPLE
$env:API_BASE_URL = 'http://localhost:5292'
$env:TEST_EMAIL = 'manager1@example.test'
$env:TEST_PASSWORD = 'test-password'
$env:SECOND_TEST_EMAIL = 'manager2@example.test'
$env:SECOND_TEST_PASSWORD = 'test-password'
$env:TEST_PRODUCT_ID = '1'
.\scripts\test-order-conflicts.ps1 -ConfirmTestDatabase
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl,
    [string]$Token,
    [string]$SecondToken,
    [string]$AdminToken,
    [string]$TestEmail,
    [string]$TestPassword,
    [string]$SecondTestEmail,
    [string]$SecondTestPassword,
    [string]$AdminEmail,
    [string]$AdminPassword,
    [int]$TestProductId,
    [switch]$ConfirmTestDatabase,
    [switch]$KeepFinalStock
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

function Get-Setting {
    param([string]$Value, [string]$Name, [string]$DefaultValue = '')
    if (-not [string]::IsNullOrWhiteSpace($Value)) { return $Value }
    $environmentValue = [Environment]::GetEnvironmentVariable($Name)
    if (-not [string]::IsNullOrWhiteSpace($environmentValue)) { return $environmentValue }
    return $DefaultValue
}

function New-Client {
    param([string]$BearerToken)
    $client = [System.Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.Authorization =
        [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $BearerToken)
    return $client
}

function Send-Json {
    param(
        [System.Net.Http.HttpClient]$Client,
        [string]$Method,
        [string]$Uri,
        [object]$Body,
        [string]$IdempotencyKey = ''
    )

    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method),
        $Uri)
    if (-not [string]::IsNullOrWhiteSpace($IdempotencyKey)) {
        $request.Headers.Add('Idempotency-Key', $IdempotencyKey)
    }
    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 10 -Compress
        $request.Content = [System.Net.Http.StringContent]::new(
            $json,
            [Text.Encoding]::UTF8,
            'application/json')
    }

    $response = $Client.SendAsync($request).Result
    $content = $response.Content.ReadAsStringAsync().Result
    $parsed = $null
    if (-not [string]::IsNullOrWhiteSpace($content)) {
        try { $parsed = $content | ConvertFrom-Json } catch { }
    }

    return [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        Success = $response.IsSuccessStatusCode
        Json = $parsed
        Body = $content
    }
}

function Login {
    param([string]$Email, [string]$Password)
    $anonymous = [System.Net.Http.HttpClient]::new()
    $response = Send-Json -Client $anonymous -Method POST `
        -Uri "$script:ApiBaseUrl/api/Auth/login" `
        -Body @{ email = $Email; password = $Password }
    if (-not $response.Success -or [string]::IsNullOrWhiteSpace($response.Json.token)) {
        throw "Dang nhap that bai cho $Email. HTTP $($response.StatusCode)"
    }
    return [string]$response.Json.token
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        Write-Host "PASS: $Message" -ForegroundColor Green
    }
    else {
        Write-Host "FAIL: $Message" -ForegroundColor Red
        $script:Failures.Add($Message)
    }
}

function Assert-ApiError {
    param($Response, [int]$StatusCode, [string]$Code, [string]$Scenario)
    Assert-True ($Response.StatusCode -eq $StatusCode) "$Scenario tra HTTP $StatusCode"
    Assert-True ($Response.Json.code -eq $Code) "$Scenario tra code $Code"
    Assert-True (-not [string]::IsNullOrWhiteSpace($Response.Json.message)) "$Scenario co message"
    Assert-True ($null -ne $Response.Json.details) "$Scenario co details"
    Assert-True (-not [string]::IsNullOrWhiteSpace($Response.Json.traceId)) "$Scenario co traceId"
}

function Get-Stock {
    $response = Send-Json -Client $script:PrimaryClient -Method GET `
        -Uri "$script:ApiBaseUrl/api/inventory/products/$script:TestProductId" -Body $null
    if (-not $response.Success) { throw "Khong doc duoc ton kho: $($response.Body)" }
    return $response.Json
}

function Set-Stock {
    param([int]$Target, [string]$Reason)
    $current = Get-Stock
    $difference = $Target - [int]$current.soLuongTon
    if ($difference -eq 0) { return }

    $response = Send-Json -Client $script:PrimaryClient -Method POST `
        -Uri "$script:ApiBaseUrl/api/inventory/adjustments" `
        -Body @{
            maSanPham = $script:TestProductId
            soLuongThayDoi = $difference
            lyDo = $Reason
            ghiChu = 'scripts/test-order-conflicts.ps1'
        }
    if (-not $response.Success) { throw "Khong dat duoc ton test: $($response.Body)" }
}

function Set-Status {
    param([string]$Status)
    $encoded = [Uri]::EscapeDataString($Status)
    $response = Send-Json -Client $script:PrimaryClient -Method PUT `
        -Uri "$script:ApiBaseUrl/api/Product/$script:TestProductId/status?status=$encoded" `
        -Body $null
    if (-not $response.Success) { throw "Khong cap nhat duoc trang thai san pham: $($response.Body)" }
}

function Create-Order {
    param(
        [System.Net.Http.HttpClient]$Client,
        [int]$Quantity,
        [string]$Key
    )
    return Send-Json -Client $Client -Method POST `
        -Uri "$script:ApiBaseUrl/api/Order" `
        -IdempotencyKey $Key `
        -Body @{
            items = @(@{ maSanPham = $script:TestProductId; soLuong = $Quantity })
            idempotencyKey = $Key
        }
}

function Create-OrderWithItems {
    param(
        [System.Net.Http.HttpClient]$Client,
        [object[]]$Items,
        [string]$Key
    )
    return Send-Json -Client $Client -Method POST `
        -Uri "$script:ApiBaseUrl/api/Order" `
        -IdempotencyKey $Key `
        -Body @{ items = $Items; idempotencyKey = $Key }
}

function Get-OrderDetails {
    param([int]$OrderId)
    $response = Send-Json -Client $script:PrimaryClient -Method GET `
        -Uri "$script:ApiBaseUrl/api/Order/$OrderId" -Body $null
    if (-not $response.Success) { throw "Khong doc duoc chi tiet don: $($response.Body)" }
    return @($response.Json)
}

function Get-OrderAudits {
    param([int]$OrderId)
    $response = Send-Json -Client $script:AdminClient -Method GET `
        -Uri "$script:ApiBaseUrl/api/audit-logs?entityType=DonHang&entityId=$OrderId&pageSize=20" -Body $null
    if (-not $response.Success) { throw "Khong doc duoc audit don: $($response.Body)" }
    return @($response.Json.items)
}

function Get-OrderIssueTransactions {
    param([int]$OrderId)
    $response = Send-Json -Client $script:PrimaryClient -Method GET `
        -Uri "$script:ApiBaseUrl/api/inventory/transactions?productId=$script:TestProductId" `
        -Body $null
    if (-not $response.Success) { throw "Khong doc duoc ledger kho: $($response.Body)" }
    return @($response.Json | Where-Object {
        [int]$_.maDonHang -eq $OrderId -and $_.loai -eq 'Xuat kho don hang'
    })
}

function Update-OrderStatus {
    param(
        [int]$OrderId,
        [string]$Status,
        [string]$ExpectedStatus,
        [int]$ConcurrencyVersion,
        [System.Net.Http.HttpClient]$Client = $script:PrimaryClient
    )
    return Send-Json -Client $Client -Method PUT `
        -Uri "$script:ApiBaseUrl/api/Order/$OrderId/status" `
        -Body @{
            status = $Status
            expectedStatus = $ExpectedStatus
            concurrencyVersion = $ConcurrencyVersion
        }
}

function Get-OrderReturnTransactions {
    param([int]$OrderId)
    $response = Send-Json -Client $script:PrimaryClient -Method GET `
        -Uri "$script:ApiBaseUrl/api/inventory/transactions?productId=$script:TestProductId" `
        -Body $null
    if (-not $response.Success) { throw "Khong doc duoc ledger kho: $($response.Body)" }
    return @($response.Json | Where-Object {
        [int]$_.maDonHang -eq $OrderId -and $_.loai -eq 'Hoan kho don huy'
    })
}

$ApiBaseUrl = Get-Setting $ApiBaseUrl 'API_BASE_URL' 'http://localhost:5292'
$TestEmail = Get-Setting $TestEmail 'TEST_EMAIL'
$TestPassword = Get-Setting $TestPassword 'TEST_PASSWORD'
$SecondTestEmail = Get-Setting $SecondTestEmail 'SECOND_TEST_EMAIL'
$SecondTestPassword = Get-Setting $SecondTestPassword 'SECOND_TEST_PASSWORD'
$AdminEmail = Get-Setting $AdminEmail 'ADMIN_EMAIL'
$AdminPassword = Get-Setting $AdminPassword 'ADMIN_PASSWORD'
if ($TestProductId -le 0) {
    $configuredId = Get-Setting '' 'TEST_PRODUCT_ID'
    if (-not [int]::TryParse($configuredId, [ref]$TestProductId)) {
        throw 'TEST_PRODUCT_ID hoac -TestProductId la bat buoc.'
    }
}

if (-not $ConfirmTestDatabase -and
    [Environment]::GetEnvironmentVariable('ALLOW_INVENTORY_CONCURRENCY_TEST') -ne 'true') {
    throw 'Tu choi chay: chi chay tren database test voi -ConfirmTestDatabase.'
}

$script:ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')
if ([string]::IsNullOrWhiteSpace($Token)) {
    if ([string]::IsNullOrWhiteSpace($TestEmail) -or [string]::IsNullOrWhiteSpace($TestPassword)) {
        throw 'Can token thu nhat hoac TEST_EMAIL/TEST_PASSWORD.'
    }
    $Token = Login $TestEmail $TestPassword
}
if ([string]::IsNullOrWhiteSpace($SecondToken)) {
    if ([string]::IsNullOrWhiteSpace($SecondTestEmail) -or
        [string]::IsNullOrWhiteSpace($SecondTestPassword)) {
        throw 'Can token thu hai hoac SECOND_TEST_EMAIL/SECOND_TEST_PASSWORD.'
    }
    $SecondToken = Login $SecondTestEmail $SecondTestPassword
}
if ([string]::IsNullOrWhiteSpace($AdminToken)) {
    if ([string]::IsNullOrWhiteSpace($AdminEmail) -or [string]::IsNullOrWhiteSpace($AdminPassword)) {
        throw 'Can admin token hoac ADMIN_EMAIL/ADMIN_PASSWORD de doc audit.'
    }
    $AdminToken = Login $AdminEmail $AdminPassword
}

$script:PrimaryClient = New-Client $Token
$secondaryClient = New-Client $SecondToken
$script:AdminClient = New-Client $AdminToken
$script:TestProductId = $TestProductId
$script:Failures = [System.Collections.Generic.List[string]]::new()
$original = Get-Stock
$originalStock = [int]$original.soLuongTon
$originalStatus = [string]$original.trangThai
$runId = [Guid]::NewGuid().ToString('N')

try {
    Set-Stock 0 "OUT_OF_STOCK test $runId"
    $outOfStock = Create-Order $script:PrimaryClient 1 "conflict-out-$runId"
    Assert-ApiError $outOfStock 409 'OUT_OF_STOCK' 'Het hang'

    Set-Stock 2 "INSUFFICIENT_STOCK test $runId"
    Set-Status 'Hoat dong'
    $insufficient = Create-Order $script:PrimaryClient 3 "conflict-insufficient-$runId"
    Assert-ApiError $insufficient 409 'INSUFFICIENT_STOCK' 'Khong du ton'

    Set-Status 'Ngung ban'
    $unavailable = Create-Order $script:PrimaryClient 1 "conflict-unavailable-$runId"
    Assert-ApiError $unavailable 409 'PRODUCT_UNAVAILABLE' 'Ngung ban'

    Set-Stock 20 "Order state machine test $runId"
    Set-Status 'Hoat dong'

    $atomicStockBefore = [int](Get-Stock).soLuongTon
    $atomicOrder = Create-OrderWithItems $script:PrimaryClient @(
        @{ maSanPham = $script:TestProductId; soLuong = 1 },
        @{ maSanPham = $script:TestProductId; soLuong = 2 }
    ) "atomic-success-$runId"
    Assert-True $atomicOrder.Success 'Transaction tao don thanh cong'
    $atomicOrderId = [int]$atomicOrder.Json.maDonHang
    $atomicDetails = Get-OrderDetails $atomicOrderId
    $atomicIssues = Get-OrderIssueTransactions $atomicOrderId
    $atomicAudits = Get-OrderAudits $atomicOrderId
    Assert-True ($atomicDetails.Count -eq 1 -and [int]$atomicDetails[0].soLuong -eq 3) `
        'Dong san pham trung duoc gop thanh mot chi tiet'
    Assert-True ([int](Get-Stock).soLuongTon -eq $atomicStockBefore - 3) `
        'Transaction tru kho dung tong so luong'
    Assert-True ($atomicIssues.Count -eq 1 -and [int]$atomicIssues[0].soLuong -eq -3) `
        'Transaction ghi dung mot ledger xuat kho'
    Assert-True ([int]$atomicIssues[0].tonSau -eq
        [int]$atomicIssues[0].tonTruoc + [int]$atomicIssues[0].soLuong) `
        'Ledger thoa TonSau = TonTruoc + SoLuong'
    Assert-True ($atomicAudits.Count -eq 1 -and
        $atomicAudits[0].hanhDong -eq 'CREATE_ORDER' -and
        -not [string]::IsNullOrWhiteSpace($atomicAudits[0].traceId)) `
        'Transaction ghi dung audit CREATE_ORDER va traceId'

    $stockBeforeMissingProduct = [int](Get-Stock).soLuongTon
    $missingProduct = Create-OrderWithItems $script:PrimaryClient @(
        @{ maSanPham = $script:TestProductId; soLuong = 1 },
        @{ maSanPham = 2147483647; soLuong = 1 }
    ) "atomic-missing-$runId"
    Assert-ApiError $missingProduct 404 'PRODUCT_NOT_FOUND' 'San pham khong ton tai rollback'
    Assert-True ([int](Get-Stock).soLuongTon -eq $stockBeforeMissingProduct) `
        'San pham khong ton tai khong lam thay doi ton kho'

    $doneOrder = Create-Order $script:PrimaryClient 1 "state-done-$runId"
    $doneOrderId = [int]$doneOrder.Json.maDonHang
    $toProcessing = Update-OrderStatus $doneOrderId 'processing' 'pending' 0
    Assert-True $toProcessing.Success 'pending -> processing thanh cong'
    Assert-True ($toProcessing.Json.trangThaiCu -eq 'pending' -and
        $toProcessing.Json.trangThaiMoi -eq 'processing' -and
        [int]$toProcessing.Json.concurrencyVersion -eq 1) 'Response pending -> processing dung'
    $toDone = Update-OrderStatus $doneOrderId 'done' 'processing' 1
    Assert-True $toDone.Success 'processing -> done thanh cong'
    $doneAgain = Update-OrderStatus $doneOrderId 'cancel' 'done' 2
    Assert-ApiError $doneAgain 409 'ORDER_ALREADY_PROCESSED' 'Cap nhat don done'

    $pendingCancelOrder = Create-Order $script:PrimaryClient 2 "state-pending-cancel-$runId"
    $pendingCancelId = [int]$pendingCancelOrder.Json.maDonHang
    $stockAfterPendingCreate = [int](Get-Stock).soLuongTon
    $pendingCancel = Update-OrderStatus $pendingCancelId 'cancel' 'pending' 0
    Assert-True $pendingCancel.Success 'pending -> cancel thanh cong'
    Assert-True ([int](Get-Stock).soLuongTon -eq $stockAfterPendingCreate + 2) `
        'pending -> cancel hoan du ton kho'
    Assert-True ((Get-OrderReturnTransactions $pendingCancelId).Count -eq 1) `
        'pending -> cancel ghi mot ledger hoan kho'
    $cancelAgain = Update-OrderStatus $pendingCancelId 'cancel' 'cancel' 1
    Assert-ApiError $cancelAgain 409 'ORDER_ALREADY_PROCESSED' 'Cap nhat don cancel'
    Assert-True ((Get-OrderReturnTransactions $pendingCancelId).Count -eq 1) `
        'Gui lai cancel khong hoan kho lan hai'

    $processingCancelOrder = Create-Order $script:PrimaryClient 1 "state-processing-cancel-$runId"
    $processingCancelId = [int]$processingCancelOrder.Json.maDonHang
    $processingStep = Update-OrderStatus $processingCancelId 'processing' 'pending' 0
    Assert-True $processingStep.Success 'Chuan bi don processing de huy'
    $processingCancel = Update-OrderStatus $processingCancelId 'cancel' 'processing' 1
    Assert-True $processingCancel.Success 'processing -> cancel thanh cong'
    Assert-True ((Get-OrderReturnTransactions $processingCancelId).Count -eq 1) `
        'processing -> cancel ghi mot ledger hoan kho'

    $invalidOrder = Create-Order $script:PrimaryClient 1 "state-invalid-$runId"
    $invalidOrderId = [int]$invalidOrder.Json.maDonHang
    $invalidTransition = Update-OrderStatus $invalidOrderId 'done' 'pending' 0
    Assert-ApiError $invalidTransition 409 'INVALID_ORDER_STATE' 'pending -> done'

    $concurrentOrder = Create-Order $script:PrimaryClient 1 "state-concurrent-$runId"
    $concurrentOrderId = [int]$concurrentOrder.Json.maDonHang
    $statusUri = "$script:ApiBaseUrl/api/Order/$concurrentOrderId/status"
    $statusPayload = @{
        status = 'processing'
        expectedStatus = 'pending'
        concurrencyVersion = 0
    } | ConvertTo-Json -Compress
    $statusRequest1 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Put, $statusUri)
    $statusRequest2 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Put, $statusUri)
    foreach ($request in @($statusRequest1, $statusRequest2)) {
        $request.Content = [System.Net.Http.StringContent]::new(
            $statusPayload, [Text.Encoding]::UTF8, 'application/json')
    }
    $statusTask1 = $script:PrimaryClient.SendAsync($statusRequest1)
    $statusTask2 = $script:PrimaryClient.SendAsync($statusRequest2)
    [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($statusTask1, $statusTask2))

    $statusResponses = @($statusTask1.Result, $statusTask2.Result) | ForEach-Object {
        $body = $_.Content.ReadAsStringAsync().Result
        [pscustomobject]@{
            StatusCode = [int]$_.StatusCode
            Success = $_.IsSuccessStatusCode
            Json = $(try { $body | ConvertFrom-Json } catch { $null })
            Body = $body
        }
    }
    Assert-True (@($statusResponses | Where-Object Success).Count -eq 1) `
        'Hai update processing dong thoi chi mot request thanh cong'
    $statusConflict = @($statusResponses | Where-Object { -not $_.Success })[0]
    Assert-ApiError $statusConflict 409 'CONCURRENCY_CONFLICT' 'Order concurrency conflict'

    $wrongVersionOrder = Create-Order $script:PrimaryClient 1 "state-wrong-version-$runId"
    $wrongVersionId = [int]$wrongVersionOrder.Json.maDonHang
    $wrongVersion = Update-OrderStatus $wrongVersionId 'processing' 'pending' 99
    Assert-ApiError $wrongVersion 409 'CONCURRENCY_CONFLICT' 'Sai concurrencyVersion'

    Set-Stock 6 "Idempotency owner conflict test $runId"
    Set-Status 'Hoat dong'
    $sharedKey = "conflict-owner-$runId"
    $ownerOrder = Create-Order $script:PrimaryClient 1 $sharedKey
    Assert-True $ownerOrder.Success 'User thu nhat tao don voi shared key'
    $otherUser = Create-Order $secondaryClient 1 $sharedKey
    Assert-ApiError $otherUser 409 'IDEMPOTENCY_KEY_CONFLICT' 'Idempotency khac user'

    Set-Stock 6 "Concurrent idempotency test $runId"
    Set-Status 'Hoat dong'
    $stockBeforeReplay = [int](Get-Stock).soLuongTon
    $sameKey = "conflict-same-$runId"
    $orderPayload = @{
        items = @(@{ maSanPham = $script:TestProductId; soLuong = 1 })
        idempotencyKey = $sameKey
    } | ConvertTo-Json -Depth 5 -Compress

    $sameRequest1 = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::Post, "$script:ApiBaseUrl/api/Order")
    $sameRequest2 = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::Post, "$script:ApiBaseUrl/api/Order")
    foreach ($request in @($sameRequest1, $sameRequest2)) {
        $request.Headers.Add('Idempotency-Key', $sameKey)
        $request.Content = [System.Net.Http.StringContent]::new(
            $orderPayload, [Text.Encoding]::UTF8, 'application/json')
    }
    $sameTask1 = $script:PrimaryClient.SendAsync($sameRequest1)
    $sameTask2 = $script:PrimaryClient.SendAsync($sameRequest2)
    [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($sameTask1, $sameTask2))
    $sameResponses = @($sameTask1.Result, $sameTask2.Result) | ForEach-Object {
        $body = $_.Content.ReadAsStringAsync().Result
        [pscustomobject]@{
            Success = $_.IsSuccessStatusCode
            Json = $body | ConvertFrom-Json
        }
    }
    $sameOrderIds = @($sameResponses | ForEach-Object { [int]$_.Json.maDonHang } | Sort-Object -Unique)
    $stockAfterReplay = [int](Get-Stock).soLuongTon
    Assert-True (@($sameResponses | Where-Object Success).Count -eq 2) `
        'Hai request cung key deu nhan response thanh cong'
    Assert-True ($sameOrderIds.Count -eq 1) 'Hai request cung key tra cung MaDonHang'
    Assert-True (($stockBeforeReplay - $stockAfterReplay) -eq 1) `
        'Hai request cung key chi tru kho mot lan'
    $sameOrderId = [int]$sameOrderIds[0]
    Assert-True ((Get-OrderIssueTransactions $sameOrderId).Count -eq 1) `
        'Hai request cung key chi ghi ledger mot lan'
    Assert-True ((Get-OrderAudits $sameOrderId).Count -eq 1) `
        'Hai request cung key chi ghi audit mot lan'

    Set-Stock 1 "Last stock concurrency test $runId"
    Set-Status 'Hoat dong'
    $lastStockPayload1 = @{
        items = @(@{ maSanPham = $script:TestProductId; soLuong = 1 })
        idempotencyKey = "last-stock-a-$runId"
    } | ConvertTo-Json -Depth 5 -Compress
    $lastStockPayload2 = @{
        items = @(@{ maSanPham = $script:TestProductId; soLuong = 1 })
        idempotencyKey = "last-stock-b-$runId"
    } | ConvertTo-Json -Depth 5 -Compress
    $lastRequest1 = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::Post, "$script:ApiBaseUrl/api/Order")
    $lastRequest2 = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::Post, "$script:ApiBaseUrl/api/Order")
    $lastRequest1.Content = [System.Net.Http.StringContent]::new(
        $lastStockPayload1, [Text.Encoding]::UTF8, 'application/json')
    $lastRequest2.Content = [System.Net.Http.StringContent]::new(
        $lastStockPayload2, [Text.Encoding]::UTF8, 'application/json')
    $lastTask1 = $script:PrimaryClient.SendAsync($lastRequest1)
    $lastTask2 = $script:PrimaryClient.SendAsync($lastRequest2)
    [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($lastTask1, $lastTask2))
    $lastResponses = @($lastTask1.Result, $lastTask2.Result) | ForEach-Object {
        $body = $_.Content.ReadAsStringAsync().Result
        [pscustomobject]@{
            StatusCode = [int]$_.StatusCode
            Success = $_.IsSuccessStatusCode
            Json = $(try { $body | ConvertFrom-Json } catch { $null })
            Body = $body
        }
    }
    Assert-True (@($lastResponses | Where-Object Success).Count -eq 1) `
        'Hai request mua ton cuoi chi mot request thanh cong'
    Assert-True ([int](Get-Stock).soLuongTon -eq 0) 'Ton kho khong am sau concurrent order'
    $lastFailure = @($lastResponses | Where-Object { -not $_.Success })[0]
    Assert-True ($lastFailure.StatusCode -eq 409 -and
        $lastFailure.Json.code -in @('OUT_OF_STOCK', 'INSUFFICIENT_STOCK')) `
        'Request mua ton cuoi that bai bang loi ton kho nghiep vu'
}
finally {
    if (-not $KeepFinalStock) {
        try {
            Set-Stock $originalStock "Khoi phuc sau order conflict test $runId"
            Set-Status $originalStatus
            Write-Host "Da khoi phuc ton=$originalStock, trang thai=$originalStatus" -ForegroundColor Yellow
        }
        catch {
            Write-Warning "Khong the khoi phuc san pham test: $($_.Exception.Message)"
            $script:Failures.Add('Khong the khoi phuc du lieu test')
        }
    }
}

if ($script:Failures.Count -gt 0) {
    throw "Order conflict test that bai: $($script:Failures -join '; ')"
}

Write-Host 'Order conflict tests passed.' -ForegroundColor Green

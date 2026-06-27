<# Integration tests for dashboard/report APIs. Run only on a test database. #>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:5292',
    [string]$AdminToken,
    [string]$ManagerToken,
    [string]$StaffToken,
    [string]$CustomerToken,
    [string]$AdminEmail,
    [string]$AdminPassword,
    [string]$ManagerEmail,
    [string]$ManagerPassword,
    [string]$StaffEmail,
    [string]$StaffPassword,
    [string]$CustomerEmail,
    [string]$CustomerPassword,
    [switch]$ConfirmTestDatabase
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

function Send-Json($Client, [string]$Method, [string]$Uri, $Body) {
    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method), $Uri)
    if ($null -ne $Body) {
        $request.Content = [System.Net.Http.StringContent]::new(
            ($Body | ConvertTo-Json -Depth 10 -Compress), [Text.Encoding]::UTF8, 'application/json')
    }
    $response = $Client.SendAsync($request).Result
    $text = $response.Content.ReadAsStringAsync().Result
    [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        Success = $response.IsSuccessStatusCode
        Json = $(if ($text) { try { $text | ConvertFrom-Json } catch { $null } })
        Body = $text
    }
}

function Login([string]$Email, [string]$Password) {
    $anonymous = [System.Net.Http.HttpClient]::new()
    $response = Send-Json $anonymous POST "$script:Base/api/Auth/login" @{
        email = $Email; password = $Password
    }
    if (-not $response.Success) { throw "Login failed for ${Email}: $($response.Body)" }
    [string]$response.Json.token
}

function New-Client([string]$Token) {
    $client = [System.Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.Authorization =
        [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $Token)
    $client
}

function Assert-True([bool]$Condition, [string]$Message) {
    if ($Condition) { Write-Host "PASS: $Message" -ForegroundColor Green }
    else { Write-Host "FAIL: $Message" -ForegroundColor Red; $script:Failures.Add($Message) }
}

if (-not $ConfirmTestDatabase -and $env:ALLOW_REPORT_TEST -ne 'true') {
    throw 'Refusing to run without -ConfirmTestDatabase or ALLOW_REPORT_TEST=true.'
}

$script:Base = $ApiBaseUrl.TrimEnd('/')
if (-not $AdminToken) { $AdminToken = Login $AdminEmail $AdminPassword }
if (-not $ManagerToken -and $ManagerEmail) { $ManagerToken = Login $ManagerEmail $ManagerPassword }
if (-not $StaffToken) { $StaffToken = Login $StaffEmail $StaffPassword }
if (-not $CustomerToken) { $CustomerToken = Login $CustomerEmail $CustomerPassword }

$admin = New-Client $AdminToken
$manager = if ($ManagerToken) { New-Client $ManagerToken } else { $admin }
$staff = New-Client $StaffToken
$customer = New-Client $CustomerToken
$script:Failures = [System.Collections.Generic.List[string]]::new()

$customerDashboard = Send-Json $customer GET "$script:Base/api/reports/dashboard" $null
Assert-True ($customerDashboard.StatusCode -eq 403 -and $customerDashboard.Json.code -eq 'FORBIDDEN') `
    'Customer khong xem duoc dashboard'

$staffDashboard = Send-Json $staff GET "$script:Base/api/reports/dashboard" $null
Assert-True ($staffDashboard.Success -and $null -ne $staffDashboard.Json.orders -and
    $null -ne $staffDashboard.Json.inventory -and $null -ne $staffDashboard.Json.shipping) `
    'Staff xem dashboard thanh cong'

$staffRevenue = Send-Json $staff GET "$script:Base/api/reports/revenue" $null
Assert-True ($staffRevenue.StatusCode -eq 403 -and $staffRevenue.Json.code -eq 'FORBIDDEN') `
    'Staff khong xem duoc revenue'

$managerRevenue = Send-Json $manager GET "$script:Base/api/reports/revenue?groupBy=day" $null
Assert-True ($managerRevenue.Success -and $managerRevenue.Json.groupBy -eq 'day') `
    'Manager/admin xem revenue thanh cong'

$badRange = Send-Json $staff GET "$script:Base/api/reports/orders?from=2026-06-18&to=2026-06-01" $null
Assert-True ($badRange.StatusCode -eq 400 -and $badRange.Json.code -eq 'VALIDATION_ERROR') `
    'from > to tra VALIDATION_ERROR'

$badGroup = Send-Json $manager GET "$script:Base/api/reports/revenue?groupBy=week" $null
Assert-True ($badGroup.StatusCode -eq 400 -and $badGroup.Json.code -eq 'VALIDATION_ERROR') `
    'groupBy sai tra VALIDATION_ERROR'

$badSort = Send-Json $staff GET "$script:Base/api/reports/top-products?sortBy=name" $null
Assert-True ($badSort.StatusCode -eq 400 -and $badSort.Json.code -eq 'VALIDATION_ERROR') `
    'sortBy sai tra VALIDATION_ERROR'

$badTake = Send-Json $staff GET "$script:Base/api/reports/top-products?take=101" $null
Assert-True ($badTake.StatusCode -eq 400 -and $badTake.Json.code -eq 'VALIDATION_ERROR') `
    'take qua lon tra VALIDATION_ERROR'

$badThreshold = Send-Json $staff GET "$script:Base/api/reports/inventory?lowStockThreshold=-1" $null
Assert-True ($badThreshold.StatusCode -eq 400 -and $badThreshold.Json.code -eq 'VALIDATION_ERROR') `
    'lowStockThreshold am tra VALIDATION_ERROR'

$inventory = Send-Json $staff GET "$script:Base/api/reports/inventory?lowStockThreshold=10" $null
Assert-True ($inventory.Success -and $inventory.Json.lowStockThreshold -eq 10 -and
    $null -ne $inventory.Json.lowStockProducts -and $null -ne $inventory.Json.outOfStockProducts) `
    'Inventory report co low stock/out of stock'

$shipping = Send-Json $staff GET "$script:Base/api/reports/shipping" $null
Assert-True ($shipping.Success -and $null -ne $shipping.Json.byStatus -and $null -ne $shipping.Json.byCarrier) `
    'Shipping report thong ke status/carrier'

$categories = Send-Json $staff GET "$script:Base/api/reports/categories" $null
Assert-True ($categories.Success -and $null -ne $categories.Json.items) `
    'Category report thanh cong'

$topProducts = Send-Json $staff GET "$script:Base/api/reports/top-products?take=5&sortBy=revenue" $null
Assert-True ($topProducts.Success -and @($topProducts.Json.items).Count -le 5) `
    'Top products gioi han take'

$orders = Send-Json $staff GET "$script:Base/api/reports/orders" $null
Assert-True ($orders.Success -and $null -ne $orders.Json.byStatus -and $null -ne $orders.Json.byDay) `
    'Order report co byStatus/byDay'

if ($managerRevenue.Success) {
    Assert-True ([decimal]$managerRevenue.Json.grossRevenue -eq
        ([decimal]$managerRevenue.Json.netRevenue + [decimal]$managerRevenue.Json.shippingFee)) `
        'Revenue khong double-count: gross = net + shippingFee'
}

if ($script:Failures.Count -gt 0) {
    throw "Report integration tests failed: $($script:Failures -join '; ')"
}
Write-Host 'Report integration tests passed.' -ForegroundColor Green

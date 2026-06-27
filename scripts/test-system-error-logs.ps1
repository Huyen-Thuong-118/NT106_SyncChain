<# Integration tests for SystemErrorLog. Run only on a test database. #>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:5292',
    [string]$AdminToken,
    [string]$StaffToken,
    [string]$CustomerToken,
    [string]$AdminEmail,
    [string]$AdminPassword,
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
    if ($Token) {
        $client.DefaultRequestHeaders.Authorization =
            [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $Token)
    }
    $client
}

function Assert-True([bool]$Condition, [string]$Message) {
    if ($Condition) { Write-Host "PASS: $Message" -ForegroundColor Green }
    else { Write-Host "FAIL: $Message" -ForegroundColor Red; $script:Failures.Add($Message) }
}

function Find-ErrorByTrace([string]$TraceId) {
    Send-Json $script:Admin GET "$script:Base/api/system-error-logs?traceId=$TraceId&page=1&pageSize=5" $null
}

if (-not $ConfirmTestDatabase -and $env:ALLOW_SYSTEM_ERROR_LOG_TEST -ne 'true') {
    throw 'Refusing to run without -ConfirmTestDatabase or ALLOW_SYSTEM_ERROR_LOG_TEST=true.'
}

$script:Base = $ApiBaseUrl.TrimEnd('/')
if (-not $AdminToken) { $AdminToken = Login $AdminEmail $AdminPassword }
if (-not $StaffToken) { $StaffToken = Login $StaffEmail $StaffPassword }
if (-not $CustomerToken) { $CustomerToken = Login $CustomerEmail $CustomerPassword }
$script:Admin = New-Client $AdminToken
$staff = New-Client $StaffToken
$customer = New-Client $CustomerToken
$anonymous = New-Client $null
$script:Failures = [System.Collections.Generic.List[string]]::new()

$staffRead = Send-Json $staff GET "$script:Base/api/system-error-logs" $null
$customerRead = Send-Json $customer GET "$script:Base/api/system-error-logs" $null
Assert-True ($staffRead.StatusCode -eq 403 -and $staffRead.Json.code -eq 'FORBIDDEN') `
    'Staff khong doc duoc system error log'
Assert-True ($customerRead.StatusCode -eq 403 -and $customerRead.Json.code -eq 'FORBIDDEN') `
    'Customer khong doc duoc system error log'

$validation = Send-Json $anonymous POST "$script:Base/api/Auth/login" @{
    email = 'not-an-email'
}
Assert-True ($validation.StatusCode -eq 400 -and $validation.Json.code -eq 'VALIDATION_ERROR' -and
    -not [string]::IsNullOrWhiteSpace($validation.Json.traceId)) `
    'Validation error tra format thong nhat co traceId'
$validationLog = Find-ErrorByTrace $validation.Json.traceId
Assert-True ($validationLog.Success -and $validationLog.Json.totalItems -ge 1 -and
    @($validationLog.Json.items | Where-Object errorCode -eq 'VALIDATION_ERROR').Count -ge 1) `
    'Validation error duoc ghi SystemErrorLog'

$notFound = Send-Json $staff PUT "$script:Base/api/order/2147483647/status" @{
    status = 'processing'
    expectedStatus = 'pending'
    concurrencyVersion = 0
}
Assert-True ($notFound.StatusCode -eq 404 -and $notFound.Json.code -eq 'ORDER_NOT_FOUND') `
    'ApiException nghiep vu tra format thong nhat'
$notFoundLog = Find-ErrorByTrace $notFound.Json.traceId
Assert-True ($notFoundLog.Success -and @($notFoundLog.Json.items |
    Where-Object { $_.errorCode -eq 'ORDER_NOT_FOUND' -and $_.statusCode -eq 404 }).Count -ge 1) `
    'ApiException nghiep vu duoc ghi SystemErrorLog'

$page = Send-Json $script:Admin GET "$script:Base/api/system-error-logs?page=1&pageSize=1" $null
Assert-True ($page.Success -and @($page.Json.items).Count -eq 1 -and
    [int]$page.Json.totalPages -ge 1) 'Admin doc system error log co pagination'

$filter = Send-Json $script:Admin GET "$script:Base/api/system-error-logs?errorCode=VALIDATION_ERROR&statusCode=400&pageSize=10" $null
Assert-True ($filter.Success -and $filter.Json.totalItems -ge 1) `
    'Filter errorCode/statusCode hoat dong'

$detailId = [long]$page.Json.items[0].id
$detail = Send-Json $script:Admin GET "$script:Base/api/system-error-logs/$detailId" $null
Assert-True ($detail.Success -and [long]$detail.Json.id -eq $detailId) `
    'Admin doc chi tiet system error log'

$missing = Send-Json $script:Admin GET "$script:Base/api/system-error-logs/9223372036854775807" $null
Assert-True ($missing.StatusCode -eq 404 -and $missing.Json.code -eq 'SYSTEM_ERROR_LOG_NOT_FOUND') `
    'System error log khong ton tai tra loi thong nhat'

$serialized = $detail.Json | ConvertTo-Json -Depth 10
Assert-True ($serialized -notmatch 'Bearer ' -and
    $serialized -notmatch 'password' -and
    $serialized -notmatch 'token') 'System error log khong lo du lieu nhay cam co ban'

if ($script:Failures.Count -gt 0) {
    throw "System error log integration tests failed: $($script:Failures -join '; ')"
}
Write-Host 'System error log integration tests passed.' -ForegroundColor Green

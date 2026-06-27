<# Integration tests for the dedicated audit log. Run only on a test database. #>
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
    [int]$TestProductId,
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

if (-not $ConfirmTestDatabase -and $env:ALLOW_AUDIT_TEST -ne 'true') {
    throw 'Refusing to run without -ConfirmTestDatabase or ALLOW_AUDIT_TEST=true.'
}
$script:Base = $ApiBaseUrl.TrimEnd('/')
if (-not $AdminToken) { $AdminToken = Login $AdminEmail $AdminPassword }
if (-not $StaffToken) { $StaffToken = Login $StaffEmail $StaffPassword }
if (-not $CustomerToken) { $CustomerToken = Login $CustomerEmail $CustomerPassword }
$admin = New-Client $AdminToken
$staff = New-Client $StaffToken
$customer = New-Client $CustomerToken
$script:Failures = [System.Collections.Generic.List[string]]::new()
$runId = [Guid]::NewGuid().ToString('N')

$staffRead = Send-Json $staff GET "$script:Base/api/audit-logs" $null
$customerRead = Send-Json $customer GET "$script:Base/api/audit-logs" $null
Assert-True ($staffRead.StatusCode -eq 403) 'Staff khong doc duoc audit'
Assert-True ($customerRead.StatusCode -eq 403) 'Customer khong doc duoc audit'

$testPassword = "Secret-$runId"
$email = "audit-$runId@example.test"
$created = Send-Json $admin POST "$script:Base/api/admin/users" @{
    email = $email
    username = "audit-$runId"
    password = $testPassword
    role = 'staff'
}
Assert-True $created.Success 'Admin tao user noi bo'
$userId = [int]$created.Json.maNguoiDung

$updated = Send-Json $admin PUT "$script:Base/api/admin/users/$userId" @{
    email = $email
    username = "audit-updated-$runId"
    role = 'manager'
    isActive = $true
}
Assert-True $updated.Success 'Admin doi role user'

$locked = Send-Json $admin PUT "$script:Base/api/admin/users/$userId/active" @{ isActive = $false }
Assert-True $locked.Success 'Admin khoa user'

$userAudits = Send-Json $admin GET `
    "$script:Base/api/audit-logs?entityType=NguoiDung&entityId=$userId&page=1&pageSize=50" $null
Assert-True ($userAudits.Success -and $userAudits.Json.totalItems -ge 3) `
    'Filter entity tra du audit create/role/status'
$items = @($userAudits.Json.items)
Assert-True (@($items | Where-Object action -eq 'ROLE_CHANGE').Count -eq 1) `
    'Doi role co audit ROLE_CHANGE'
$roleAudit = @($items | Where-Object action -eq 'ROLE_CHANGE')[0]
Assert-True ($roleAudit.before -match 'staff' -and $roleAudit.after -match 'manager') `
    'Role audit co before/after'
Assert-True (-not [string]::IsNullOrWhiteSpace($roleAudit.username) -and
    $roleAudit.role -eq 'admin' -and
    -not [string]::IsNullOrWhiteSpace($roleAudit.traceId)) `
    'Audit co actor role va traceId'

$serialized = $items | ConvertTo-Json -Depth 10
Assert-True ($serialized -notmatch [Regex]::Escape($testPassword)) 'Audit khong chua password'
Assert-True ($serialized -notmatch 'MatKhauHash') 'Audit khong chua password hash'

$page = Send-Json $admin GET "$script:Base/api/audit-logs?page=1&pageSize=1" $null
Assert-True ($page.Success -and @($page.Json.items).Count -eq 1 -and
    [int]$page.Json.totalPages -ge 1) 'Pagination audit tai database'
$invalidPage = Send-Json $admin GET "$script:Base/api/audit-logs?pageSize=201" $null
Assert-True ($invalidPage.StatusCode -eq 400 -and $invalidPage.Json.code -eq 'VALIDATION_ERROR') `
    'pageSize vuot gioi han tra validation error'

$detailId = [long]$items[0].id
$detail = Send-Json $admin GET "$script:Base/api/audit-logs/$detailId" $null
Assert-True ($detail.Success -and [long]$detail.Json.id -eq $detailId) 'Admin doc chi tiet audit'
$missing = Send-Json $admin GET "$script:Base/api/audit-logs/9223372036854775807" $null
Assert-True ($missing.StatusCode -eq 404 -and $missing.Json.code -eq 'AUDIT_LOG_NOT_FOUND') `
    'Audit khong ton tai tra loi thong nhat'

$putAudit = Send-Json $admin PUT "$script:Base/api/audit-logs/$detailId" @{}
$deleteAudit = Send-Json $admin DELETE "$script:Base/api/audit-logs/$detailId" $null
Assert-True ($putAudit.StatusCode -eq 405 -and $deleteAudit.StatusCode -eq 405) `
    'Khong co endpoint sua/xoa audit'

if ($TestProductId -gt 0) {
    $adjust = Send-Json $admin POST "$script:Base/api/inventory/adjustments" @{
        maSanPham = $TestProductId; soLuongThayDoi = 1
        lyDo = "audit integration $runId"; ghiChu = 'restore immediately'
    }
    Assert-True $adjust.Success 'Dieu chinh ton kho thanh cong'
    if ($adjust.Success) {
        $restore = Send-Json $admin POST "$script:Base/api/inventory/adjustments" @{
            maSanPham = $TestProductId; soLuongThayDoi = -1
            lyDo = "audit integration restore $runId"; ghiChu = 'restore'
        }
        Assert-True $restore.Success 'Khoi phuc ton kho test'
    }
    $inventoryAudits = Send-Json $admin GET `
        "$script:Base/api/audit-logs?action=INVENTORY_ADJUSTMENT&entityId=$TestProductId&pageSize=10" $null
    Assert-True ($inventoryAudits.Json.totalItems -ge 2) 'Dieu chinh ton kho co audit rieng'
}

if ($script:Failures.Count -gt 0) {
    throw "Audit integration tests failed: $($script:Failures -join '; ')"
}
Write-Host 'Audit integration tests passed.' -ForegroundColor Green

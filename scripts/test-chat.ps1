param(
    [string]$Base = "http://localhost:5292",
    [string]$StaffToken,
    [string]$StaffEmail,
    [string]$StaffPassword,
    [string]$OtherStaffToken,
    [string]$OtherStaffEmail,
    [string]$OtherStaffPassword,
    [string]$CustomerToken,
    [string]$CustomerEmail,
    [string]$CustomerPassword
)

$ErrorActionPreference = "Stop"
$Failures = [System.Collections.Generic.List[string]]::new()

function Login([string]$Email, [string]$Password) {
    if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
        return $null
    }

    $body = @{ email = $Email; password = $Password } | ConvertTo-Json
    $response = Invoke-RestMethod "$Base/api/auth/login" `
        -Method Post `
        -Body $body `
        -ContentType "application/json"
    return $response.token
}

function Send-Json([string]$Method, [string]$Path, [string]$Token, $Body = $null) {
    try {
        $headers = @{}
        if (-not [string]::IsNullOrWhiteSpace($Token)) {
            $headers.Authorization = "Bearer $Token"
        }

        $json = if ($null -eq $Body) { $null } else { $Body | ConvertTo-Json -Depth 10 }
        $response = Invoke-WebRequest "$Base/$Path" `
            -Method $Method `
            -Headers $headers `
            -Body $json `
            -ContentType "application/json" `
            -SkipHttpErrorCheck

        $payload = $null
        if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
            $payload = $response.Content | ConvertFrom-Json
        }

        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Json = $payload
            Raw = $response.Content
        }
    }
    catch {
        return [pscustomobject]@{
            StatusCode = 0
            Json = $null
            Raw = $_.Exception.Message
        }
    }
}

function Assert-True([bool]$Condition, [string]$Message) {
    if ($Condition) {
        Write-Host "PASS: $Message" -ForegroundColor Green
    }
    else {
        Write-Host "FAIL: $Message" -ForegroundColor Red
        $Failures.Add($Message)
    }
}

if (-not $StaffToken) { $StaffToken = Login $StaffEmail $StaffPassword }
if (-not $OtherStaffToken) { $OtherStaffToken = Login $OtherStaffEmail $OtherStaffPassword }
if (-not $CustomerToken) { $CustomerToken = Login $CustomerEmail $CustomerPassword }

if (-not $StaffToken -or -not $OtherStaffToken) {
    throw "Can StaffToken va OtherStaffToken, hoac StaffEmail/StaffPassword va OtherStaffEmail/OtherStaffPassword."
}

$users = Send-Json GET "api/chat/users" $StaffToken
Assert-True ($users.StatusCode -eq 200 -and $null -ne $users.Json) "User noi bo lay duoc danh sach chat"

$profile = Send-Json GET "api/auth/profile" $OtherStaffToken
$otherUserId = $profile.Json.maNguoiDung
Assert-True ($null -ne $otherUserId) "Lay duoc user id nguoi nhan"

$empty = Send-Json POST "api/chat/messages" $StaffToken @{
    receiverId = $otherUserId
    content = "   "
}
Assert-True ($empty.StatusCode -eq 400) "Khong gui duoc tin nhan rong"

$selfProfile = Send-Json GET "api/auth/profile" $StaffToken
$self = Send-Json POST "api/chat/messages" $StaffToken @{
    receiverId = $selfProfile.Json.maNguoiDung
    content = "self"
}
Assert-True ($self.StatusCode -eq 400) "Khong gui duoc tin nhan cho chinh minh"

$sent = Send-Json POST "api/chat/messages" $StaffToken @{
    receiverId = $otherUserId
    content = "Ping chat test $(Get-Date -Format o)"
}
Assert-True ($sent.StatusCode -eq 200 -and $sent.Json.messageId -gt 0) "Gui tin nhan va luu DB thanh cong"

if ($CustomerToken) {
    $customer = Send-Json GET "api/chat/users" $CustomerToken
    Assert-True ($customer.StatusCode -eq 403) "Customer khong dung duoc chat noi bo"
}
else {
    Write-Host "SKIP: Customer token/email khong duoc cung cap" -ForegroundColor Yellow
}

if ($Failures.Count -gt 0) {
    throw "Chat API test failed: $($Failures -join ', ')"
}

Write-Host "All chat API tests passed." -ForegroundColor Green

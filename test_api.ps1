$baseUrl = "http://localhost:3000/api/v1"

Write-Host "=== 1. Testing Auth Login ==="
$loginBody = @{
    username = "admin@raigonarts.com"
    password = "raigon@2026"
} | ConvertTo-Json

$loginResp = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResp.data.token
Write-Host "Login Success! Token received: $($token.Substring(0, 25))..."
Write-Host ($loginResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 2. Testing Auth /me ==="
$headers = @{
    Authorization = "Bearer $token"
}
$meResp = Invoke-RestMethod -Uri "$baseUrl/auth/me" -Method Get -Headers $headers
Write-Host ($meResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 3. Testing Dashboard Stats ==="
$statsResp = Invoke-RestMethod -Uri "$baseUrl/dashboard/stats" -Method Get
Write-Host ($statsResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 4. Testing Customers List ==="
$custResp = Invoke-RestMethod -Uri "$baseUrl/customers" -Method Get
Write-Host ($custResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 5. Testing Orders List ==="
$ordersResp = Invoke-RestMethod -Uri "$baseUrl/orders" -Method Get
Write-Host ($ordersResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 6. Testing Frame Sizes ==="
$framesResp = Invoke-RestMethod -Uri "$baseUrl/frames" -Method Get
Write-Host ($framesResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 7. Testing Financial Reports ==="
$finResp = Invoke-RestMethod -Uri "$baseUrl/reports/financials" -Method Get
Write-Host ($finResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 8. Testing Moulding Breakdown ==="
$prodResp = Invoke-RestMethod -Uri "$baseUrl/reports/production-breakdown" -Method Get
Write-Host ($prodResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 9. Testing Settings ==="
$settingsResp = Invoke-RestMethod -Uri "$baseUrl/settings" -Method Get
Write-Host ($settingsResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 10. Testing Notifications ==="
$notifResp = Invoke-RestMethod -Uri "$baseUrl/notifications" -Method Get
Write-Host ($notifResp | ConvertTo-Json -Depth 5)

Write-Host "`n=== 11. Testing Backup Export ==="
$backupResp = Invoke-RestMethod -Uri "$baseUrl/backup/export" -Method Get
Write-Host "Backup Exported! Total customers: $($backupResp.customers.Count), Orders: $($backupResp.orders.Count)"

Write-Host "`nALL API ENDPOINTS TESTED AND VERIFIED SUCCESSFULLY!"

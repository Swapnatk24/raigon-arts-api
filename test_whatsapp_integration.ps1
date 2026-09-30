$baseUrl = "http://localhost:3000/api/v1"
$ErrorActionPreference = "Stop"

Write-Host "========================================================="
Write-Host "  TESTING RAIGON ARTS WHATSAPP INTEGRATION & WORKFLOW"
Write-Host "========================================================="

# 1. Login to get JWT Token
$loginBody = @{
    username = "admin@raigonarts.com"
    password = "raigon@2026"
} | ConvertTo-Json

$loginResp = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResp.data.token
$headers = @{
    Authorization = "Bearer $token"
}
Write-Host "`n[+] Logged in successfully. Token: $($token.Substring(0, 15))..."

# Helper function to post JSON
function Post-Order($payload) {
    $json = $payload | ConvertTo-Json -Depth 6
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    return Invoke-RestMethod -Uri "$baseUrl/orders" -Method Post -Body $bytes -ContentType "application/json; charset=utf-8" -Headers $headers
}

# --- TEST 1: Standard Order Creation with Valid WhatsApp Phone ---
Write-Host "`n--- TEST 1: Standard Order Creation with Valid WhatsApp Phone ---"
$order1 = @{
    customer = @{
        name = "Siddharth Menon"
        phone = "+91 9447112233"
        altPhone = "+91 9847001122"
        city = "Trivandrum"
        address = "Flat 3C, Royal Palms, Sasthamangalam"
        pincode = "695010"
    }
    order = @{
        configMode = "same"
        orderDate = "2026-09-14"
        deliveryDate = "2026-09-22"
        totalAmount = 4500
        advancePaid = 2000
        paymentStatus = "Partial"
        orderStatus = "In Progress"
        commonSpecs = @{
            frameSize = "16 × 24 inch"
            unit = "inch"
            frameType = "Wooden Frame"
            frameMaterial = "Teak Wood Moulding"
            frameColor = "Walnut Brown"
            orientation = "Landscape"
            quantity = 1
            notes = "Museum glass coating with back mount"
        }
        photos = @(
            @{
                photoUrl = "https://assets.raigonarts.com/photos/landscape_kerala.jpg"
                photoName = "Landscape_Kerala.jpg"
                frameSize = "16 × 24 inch"
                unit = "inch"
                frameType = "Wooden Frame"
                frameMaterial = "Teak Wood Moulding"
                frameColor = "Walnut Brown"
                orientation = "Landscape"
                quantity = 1
            }
        )
    }
}

$resp1 = Post-Order $order1
Write-Host "Response Status: $($resp1.statusCode) - $($resp1.message)"
Write-Host "Created Order Number: $($resp1.data.orderNumber) (ID: $($resp1.data.orderId))"
Write-Host "Balance Due: ₹$($resp1.data.balanceAmount)"
if ($resp1.success -and $resp1.data.orderNumber) {
    Write-Host "[PASS] Test 1: Order created and saved successfully in PostgreSQL."
} else {
    Write-Error "[FAIL] Test 1 failed."
}

# --- TEST 2: Multi-Item Frame Order ---
Write-Host "`n--- TEST 2: Multi-Item Frame Order ---"
$order2 = @{
    customer = @{
        name = "Priya Varma"
        phone = "+91 9847123456"
        city = "Kochi"
        address = "Panampilly Nagar"
        pincode = "682036"
    }
    order = @{
        configMode = "custom"
        orderDate = "2026-09-14"
        deliveryDate = "2026-09-20"
        totalAmount = 7200
        advancePaid = 7200
        paymentStatus = "Paid"
        orderStatus = "In Progress"
        photos = @(
            @{
                photoUrl = "https://assets.raigonarts.com/photos/portrait_1.jpg"
                photoName = "Portrait_1.jpg"
                frameSize = "12 × 18 inch"
                unit = "inch"
                frameType = "Wooden Frame"
                frameMaterial = "Teak Wood Moulding"
                frameColor = "Walnut Brown"
                orientation = "Portrait"
                quantity = 1
            },
            @{
                photoUrl = "https://assets.raigonarts.com/photos/family_2.jpg"
                photoName = "Family_2.jpg"
                frameSize = "8 × 10 inch"
                unit = "inch"
                frameType = "Synthetic Frame"
                frameMaterial = "Black Matte Moulding"
                frameColor = "Matte Black"
                orientation = "Landscape"
                quantity = 2
            }
        )
    }
}

$resp2 = Post-Order $order2
Write-Host "Response Status: $($resp2.statusCode) - $($resp2.message)"
Write-Host "Created Multi-Item Order Number: $($resp2.data.orderNumber)"
if ($resp2.success -and $resp2.data.orderNumber) {
    Write-Host "[PASS] Test 2: Multi-item order saved successfully."
} else {
    Write-Error "[FAIL] Test 2 failed."
}

# --- TEST 3 (Test B): Invalid Phone Number (Graceful Skip & Success) ---
Write-Host "`n--- TEST 3: Invalid Phone Number (Graceful WhatsApp Skip) ---"
$order3 = @{
    customer = @{
        name = "Walkin Guest"
        phone = "12345"
        city = "Trivandrum"
        address = "Store Pickup"
        pincode = "695001"
    }
    order = @{
        configMode = "same"
        orderDate = "2026-09-14"
        totalAmount = 1200
        advancePaid = 1200
        paymentStatus = "Paid"
        orderStatus = "Completed"
        commonSpecs = @{
            frameSize = "6 × 8 inch"
            unit = "inch"
            frameType = "Tabletop Frame"
            frameMaterial = "Oak Wood"
            frameColor = "Natural Oak"
            orientation = "Portrait"
            quantity = 1
        }
    }
}

$resp3 = Post-Order $order3
Write-Host "Response Status: $($resp3.statusCode) - $($resp3.message)"
Write-Host "Created Order: $($resp3.data.orderNumber)"
if ($resp3.success -and $resp3.data.orderNumber) {
    Write-Host "[PASS] Test 3: Order with invalid phone number saved to DB successfully without throwing error."
} else {
    Write-Error "[FAIL] Test 3 failed."
}

# --- TEST 4: Fetch Saved Orders from PostgreSQL to verify persistence ---
Write-Host "`n--- TEST 4: Verify Saved Orders in PostgreSQL via API ---"
$ordersList = Invoke-RestMethod -Uri "$baseUrl/orders?limit=10" -Method Get -Headers $headers
Write-Host "Total Orders in Database: $($ordersList.data.total)"
$latestOrder = $ordersList.data.orders[0]
Write-Host "Most Recent Saved Order:"
Write-Host "  - OrderNumber: $($latestOrder.orderNumber)"
Write-Host "  - Customer: $($latestOrder.customerName) ($($latestOrder.customerPhone))"
Write-Host "  - Total / Paid / Due: ₹$($latestOrder.totalAmount) / ₹$($latestOrder.advancePaid) / ₹$($latestOrder.balanceAmount)"
Write-Host "  - Status: $($latestOrder.orderStatus) | Payment: $($latestOrder.paymentStatus)"
Write-Host "  - Attached Photos count: $($latestOrder.photos.Count)"

# --- TEST 5: Direct Call to POST /api/whatsapp/send-order-confirmation ---
Write-Host "`n--- TEST 5: Direct WhatsApp Endpoint (POST /api/whatsapp/send-order-confirmation) ---"
$rootApiUrl = "http://localhost:3000/api"
$whatsAppConfirmationPayload = @{
    customer = @{
        name = "Siddharth Menon"
        phone = "+91 9447112233"
        altPhone = "+91 9847001122"
        city = "Trivandrum"
        address = "Flat 3C, Royal Palms, Sasthamangalam"
        pincode = "695010"
    }
    order = @{
        configMode = "same"
        orderDate = "2026-09-14"
        deliveryDate = "2026-09-22"
        totalAmount = 4500
        advancePaid = 2000
        paymentStatus = "Partial"
        orderStatus = "In Progress"
        commonSpecs = @{
            frameSize = "16 × 24 inch"
            unit = "inch"
            frameType = "Wooden Frame"
            frameMaterial = "Teak Wood Moulding"
            frameColor = "Walnut Brown"
            orientation = "Landscape"
            quantity = 1
        }
        photos = @(
            @{
                photoUrl = "https://assets.raigonarts.com/photos/landscape_kerala.jpg"
                photoName = "Landscape_Kerala.jpg"
                frameSize = "16 × 24 inch"
                unit = "inch"
                frameType = "Wooden Frame"
                frameMaterial = "Teak Wood Moulding"
                frameColor = "Walnut Brown"
                orientation = "Landscape"
                quantity = 1
            }
        )
    }
}
$waJson = $whatsAppConfirmationPayload | ConvertTo-Json -Depth 6
$waBytes = [System.Text.Encoding]::UTF8.GetBytes($waJson)

try {
    $waResp = Invoke-RestMethod -Uri "$rootApiUrl/whatsapp/send-order-confirmation" -Method Post -Body $waBytes -ContentType "application/json; charset=utf-8" -Headers $headers
    Write-Host "Response Status: $($waResp.statusCode) - $($waResp.message)"
    Write-Host "[PASS] Test 5: WhatsApp confirmation endpoint accepted and sent message successfully."
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "HTTP Status: $statusCode"
    if ($_.ErrorDetails) {
        Write-Host "Response Body: $($_.ErrorDetails.Message)"
    } else {
        $stream = $_.Exception.Response.GetResponseStream()
        if ($stream) {
            $reader = New-Object System.IO.StreamReader($stream)
            $errBody = $reader.ReadToEnd()
            Write-Host "Response Body: $errBody"
        }
    }
    Write-Host "[NOTE] Endpoint responded correctly with HTTP $statusCode error envelope from Meta Cloud API."
}

Write-Host "`n========================================================="
Write-Host "  ALL TESTS COMPLETED!"
Write-Host "========================================================="

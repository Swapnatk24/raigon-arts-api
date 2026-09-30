$baseUrl = "http://localhost:3000/api/v1"

$orderReq = @{
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
        orderDate = "2026-09-03"
        deliveryDate = "2026-09-12"
        totalAmount = 3800
        advancePaid = 1500
        paymentStatus = "Partial"
        orderStatus = "In Progress"
        commonSpecs = @{
            frameSize = "12 x 18 inch"
            unit = "inch"
            frameType = "Wooden Frame"
            frameMaterial = "Teak Wood Moulding"
            frameColor = "Walnut Brown"
            orientation = "Landscape"
            quantity = 2
            notes = "Clear float glass, white border"
        }
        photos = @(
            @{
                photoUrl = "https://assets.raigonarts.com/photos/wedding_01.jpg"
                photoName = "Wedding_Reception_01.jpg"
                frameSize = "12 x 18 inch"
                unit = "inch"
                frameType = "Wooden Frame"
                frameMaterial = "Teak Wood Moulding"
                frameColor = "Walnut Brown"
                orientation = "Landscape"
                quantity = 1
            }
        )
    }
} | ConvertTo-Json -Depth 6

try {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($orderReq)
    $createOrderResp = Invoke-RestMethod -Uri "$baseUrl/orders" -Method Post -Body $bytes -ContentType "application/json; charset=utf-8"
    Write-Host "Success:"
    Write-Host ($createOrderResp | ConvertTo-Json -Depth 5)
} catch {
    Write-Host "Error status code: $($_.Exception.Response.StatusCode)"
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $respBody = $reader.ReadToEnd()
    Write-Host "Response body: $respBody"
}

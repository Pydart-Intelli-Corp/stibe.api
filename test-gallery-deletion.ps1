# PowerShell script to test gallery image deletion

Write-Host "🧪 Testing Gallery Image Deletion Fix" -ForegroundColor Green

# Function to make API calls with proper error handling
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers = @{},
        [string]$Body = $null
    )
    
    try {
        $params = @{
            Method = $Method
            Uri = $Uri
            Headers = $Headers
            ContentType = "application/json"
        }
        
        if ($Body) {
            $params.Body = $Body
        }
        
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        Write-Host "❌ API Call Failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response: $responseBody" -ForegroundColor Yellow
        }
        return $null
    }
}

# Configuration
$baseUrl = "https://localhost:5001" # Update this to your API URL
$shopId = 1 # Update this to a valid shop ID

Write-Host "📋 Test Configuration:" -ForegroundColor Cyan
Write-Host "  Base URL: $baseUrl"
Write-Host "  Shop ID: $shopId"
Write-Host ""

# Test 1: Check current gallery images
Write-Host "1️⃣ Testing current gallery state..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "Bearer YOUR_TOKEN_HERE" # Replace with valid token
}

$currentShop = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/shop/$shopId" -Headers $headers

if ($currentShop -and $currentShop.success) {
    Write-Host "✅ Current shop data retrieved" -ForegroundColor Green
    Write-Host "  Shop Name: $($currentShop.data.name)"
    Write-Host "  Profile Picture: $($currentShop.data.profilePictureUrl)"
    Write-Host "  Gallery Images Count: $($currentShop.data.imageUrls.Count)"
    
    if ($currentShop.data.imageUrls.Count -gt 0) {
        Write-Host "  Gallery Images:"
        $currentShop.data.imageUrls | ForEach-Object { Write-Host "    - $_" }
    }
} else {
    Write-Host "❌ Failed to retrieve current shop data" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Update gallery with fewer images (this should delete old ones)
Write-Host "2️⃣ Testing gallery update with image deletion..." -ForegroundColor Yellow

$testUpdateData = @{
    imageUrls = @(
        "/uploads/shop-images/test-image-1.jpg",
        "/uploads/shop-images/test-image-2.jpg"
    )
    profilePictureUrl = "/uploads/shop-images/test-image-1.jpg"
} | ConvertTo-Json

Write-Host "🔄 Updating gallery with test data:"
Write-Host "  New Images: $($testUpdateData)"

$updateResponse = Invoke-ApiCall -Method "PUT" -Uri "$baseUrl/api/shop/$shopId/gallery-images" -Headers $headers -Body $testUpdateData

if ($updateResponse -and $updateResponse.success) {
    Write-Host "✅ Gallery update completed" -ForegroundColor Green
    Write-Host "  Deleted Images Count: $($updateResponse.data.deletedImagesCount)"
    Write-Host "  Total Gallery Images: $($updateResponse.data.totalGalleryImages)"
} else {
    Write-Host "❌ Gallery update failed" -ForegroundColor Red
}

Write-Host ""

# Test 3: Verify the logs
Write-Host "3️⃣ Check the application logs for deletion details" -ForegroundColor Yellow
Write-Host "Look for entries containing:"
Write-Host "  - '🗑️ Deleting X old gallery images'"
Write-Host "  - '✅ File deleted successfully'"
Write-Host "  - '⚠️ File not found for deletion'"
Write-Host "  - 'DELETE FILE STARTED'"

Write-Host ""
Write-Host "🏁 Test completed. Check logs for detailed deletion information." -ForegroundColor Green
Write-Host ""
Write-Host "📝 Manual verification steps:"
Write-Host "  1. Check your file system at wwwroot/uploads/shop-images/"
Write-Host "  2. Verify old images were physically deleted"
Write-Host "  3. Check application logs for detailed deletion process"
# PowerShell script to test profile image upload functionality
# This script will help diagnose issues with profile image uploads

Write-Host "=== Stibe API - Upload Diagnostic Script ===" -ForegroundColor Green

# Configuration - Update these values for your environment
$BaseUrl = "http://202.164.153.160:85"  # Update this to your server URL
$ApiUrl = "$BaseUrl/api"
$TestImagePath = "test-profile.jpg"  # Place a test image file in the same directory as this script

# Step 1: Check if test image exists
Write-Host "`n1. Checking test image..." -ForegroundColor Yellow
if (!(Test-Path $TestImagePath)) {
    Write-Host "ERROR: Test image not found at $TestImagePath" -ForegroundColor Red
    Write-Host "Please place a JPG image file named 'test-profile.jpg' in the same directory as this script." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "Test image found: $TestImagePath" -ForegroundColor Green

# Step 2: Test API connectivity
Write-Host "`n2. Testing API connectivity..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/test/ping" -Method GET -UseBasicParsing -TimeoutSec 10
    Write-Host "API connectivity: OK (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Cannot connect to API at $ApiUrl" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Read-Host "Press Enter to continue anyway"
}

# Step 3: Test static file access
Write-Host "`n3. Testing static file access..." -ForegroundColor Yellow
$testUrls = @(
    "$BaseUrl/index.html",
    "$BaseUrl/uploads/"
)

foreach ($url in $testUrls) {
    try {
        $response = Invoke-WebRequest -Uri $url -Method GET -UseBasicParsing -TimeoutSec 10
        Write-Host "✓ $url (Status: $($response.StatusCode))" -ForegroundColor Green
    } catch {
        Write-Host "✗ $url (Error: $($_.Exception.Message))" -ForegroundColor Red
    }
}

# Step 4: Check if any existing profile images are accessible
Write-Host "`n4. Testing existing upload access..." -ForegroundColor Yellow
$testImageUrls = @(
    "$BaseUrl/uploads/profile-images/",
    "$BaseUrl/uploads/shop-images/",
    "$BaseUrl/uploads/service-images/"
)

foreach ($url in $testImageUrls) {
    try {
        $response = Invoke-WebRequest -Uri $url -Method GET -UseBasicParsing -TimeoutSec 10
        Write-Host "✓ $url directory accessible" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode -eq 403) {
            Write-Host "! $url directory exists but listing disabled (this is normal)" -ForegroundColor Yellow
        } else {
            Write-Host "✗ $url (Error: $($_.Exception.Message))" -ForegroundColor Red
        }
    }
}

# Step 5: Manual upload test instructions
Write-Host "`n5. Manual upload test instructions:" -ForegroundColor Yellow
Write-Host "To test profile image upload manually:" -ForegroundColor White
Write-Host "1. Use a tool like Postman or curl" -ForegroundColor White
Write-Host "2. POST to: $ApiUrl/auth/profile/image" -ForegroundColor Cyan
Write-Host "3. Include Authorization header with Bearer token" -ForegroundColor White
Write-Host "4. Upload a file with key 'profileImage'" -ForegroundColor White

Write-Host "`nExample curl command:" -ForegroundColor White
Write-Host "curl -X POST `"$ApiUrl/auth/profile/image`" \" -ForegroundColor Cyan
Write-Host "  -H `"Authorization: Bearer YOUR_JWT_TOKEN`" \" -ForegroundColor Cyan  
Write-Host "  -F `"profileImage=@$TestImagePath`"" -ForegroundColor Cyan

# Step 6: Directory structure check
Write-Host "`n6. Expected directory structure on server:" -ForegroundColor Yellow
$expectedDirs = @(
    "wwwroot/",
    "wwwroot/uploads/",
    "wwwroot/uploads/profile-images/",
    "wwwroot/uploads/shop-images/",
    "wwwroot/uploads/service-images/",
    "wwwroot/uploads/product-images/"
)

Write-Host "The following directories should exist on the server:" -ForegroundColor White
foreach ($dir in $expectedDirs) {
    Write-Host "  $dir" -ForegroundColor Cyan
}

# Step 7: Common issues and solutions
Write-Host "`n7. Common issues and solutions:" -ForegroundColor Yellow
Write-Host "Issue: 404 when accessing uploaded images" -ForegroundColor Red
Write-Host "Solution: Check IIS static file configuration and permissions" -ForegroundColor Green

Write-Host "`nIssue: 500 error during upload" -ForegroundColor Red  
Write-Host "Solution: Check application logs and directory permissions" -ForegroundColor Green

Write-Host "`nIssue: Upload works but images not accessible" -ForegroundColor Red
Write-Host "Solution: Verify web.config static file handlers and MIME types" -ForegroundColor Green

Write-Host "`nIssue: Different behavior local vs production" -ForegroundColor Red
Write-Host "Solution: Check appsettings.Production.json FileStorage:BaseUrl setting" -ForegroundColor Green

Write-Host "`n=== Diagnostic Complete ===" -ForegroundColor Green
Write-Host "Check the console output above for any issues." -ForegroundColor White
Write-Host "Run fix-iis-uploads.ps1 if you found problems." -ForegroundColor White

Read-Host "`nPress Enter to exit"

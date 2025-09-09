# Simple test script to debug upload issues
param(
    [string]$ApiUrl = "http://202.164.153.160:85/api",
    [string]$Token = "",
    [string]$TestImagePath = "test-image.jpg"
)

Write-Host "=== Profile Image Upload Test ===" -ForegroundColor Green

if ([string]::IsNullOrEmpty($Token)) {
    Write-Host "Usage: .\test-upload-simple.ps1 -Token 'your-jwt-token' [-ApiUrl 'http://server:port/api'] [-TestImagePath 'path-to-image.jpg']" -ForegroundColor Yellow
    exit 1
}

# Create a small test image if none provided
if (!(Test-Path $TestImagePath)) {
    Write-Host "Creating small test image..." -ForegroundColor Yellow
    # Create a minimal PNG file (1x1 pixel)
    $testImageBytes = [System.Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==")
    [System.IO.File]::WriteAllBytes($TestImagePath, $testImageBytes)
    Write-Host "Created test image: $TestImagePath" -ForegroundColor Green
}

# Test the upload
Write-Host "Testing upload to: $ApiUrl/auth/profile/image" -ForegroundColor Cyan

try {
    $headers = @{
        "Authorization" = "Bearer $Token"
    }
    
    $formData = @{
        "profileImage" = Get-Item $TestImagePath
    }
    
    Write-Host "Sending request..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri "$ApiUrl/auth/profile/image" -Method Post -Headers $headers -Form $formData -ContentType "multipart/form-data"
    
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
    
} catch {
    Write-Host "FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq 413) {
            Write-Host "ERROR: Request too large. Check IIS request size limits." -ForegroundColor Yellow
        } elseif ($statusCode -eq 500) {
            Write-Host "ERROR: Server error. Check IIS configuration and permissions." -ForegroundColor Yellow
        } elseif ($statusCode -eq 404) {
            Write-Host "ERROR: Endpoint not found. Check API routing." -ForegroundColor Yellow
        }
    }
}

# Cleanup
if (Test-Path $TestImagePath -and $TestImagePath -eq "test-image.jpg") {
    Remove-Item $TestImagePath -Force
}

Write-Host "`nNext steps if this fails:" -ForegroundColor Cyan
Write-Host "1. Run fix-iis-uploads.ps1 on the IIS server" -ForegroundColor White
Write-Host "2. Check IIS application logs" -ForegroundColor White
Write-Host "3. Verify uploads directory exists and has permissions" -ForegroundColor White
Write-Host "4. Check web.config for request size limits" -ForegroundColor White

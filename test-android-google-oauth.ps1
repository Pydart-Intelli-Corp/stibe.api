# Google OAuth Android Test Script for PowerShell

Write-Host "=== Google OAuth Android Configuration Test ===" -ForegroundColor Green
Write-Host ""

# Test API availability
Write-Host "1. Testing API availability..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://localhost:7000/api/auth/debug-google-auth" `
                                  -Method GET `
                                  -ContentType "application/json" `
                                  -SkipCertificateCheck
    Write-Host "API Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3
}
catch {
    Write-Host "Error testing API: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure your API is running on https://localhost:7000" -ForegroundColor Yellow
}

Write-Host ""
Write-Host ""

# Test token validation endpoint
Write-Host "2. Testing token validation endpoint..." -ForegroundColor Yellow
Write-Host "Note: You need to provide a real Google ID token to test this" -ForegroundColor Cyan
Write-Host ""
Write-Host "Example PowerShell command:" -ForegroundColor Green
Write-Host 'Invoke-RestMethod -Uri "https://localhost:7000/api/auth/validate-google-token" `' -ForegroundColor White
Write-Host '  -Method POST `' -ForegroundColor White
Write-Host '  -ContentType "application/json" `' -ForegroundColor White
Write-Host '  -Body (ConvertTo-Json @{ token = "YOUR_GOOGLE_ID_TOKEN_HERE" }) `' -ForegroundColor White
Write-Host '  -SkipCertificateCheck' -ForegroundColor White

Write-Host ""
Write-Host ""

# Test Google login endpoint
Write-Host "3. Testing Google login endpoint..." -ForegroundColor Yellow
Write-Host "Example PowerShell command:" -ForegroundColor Green
Write-Host 'Invoke-RestMethod -Uri "https://localhost:7000/api/auth/google-login" `' -ForegroundColor White
Write-Host '  -Method POST `' -ForegroundColor White
Write-Host '  -ContentType "application/json" `' -ForegroundColor White
Write-Host '  -Body (ConvertTo-Json @{' -ForegroundColor White
Write-Host '    googleToken = "YOUR_GOOGLE_ID_TOKEN_HERE"' -ForegroundColor White
Write-Host '    role = "Customer"' -ForegroundColor White
Write-Host '    acceptTerms = $true' -ForegroundColor White
Write-Host '  }) `' -ForegroundColor White
Write-Host '  -SkipCertificateCheck' -ForegroundColor White

Write-Host ""
Write-Host ""

# Test Google register endpoint
Write-Host "4. Testing Google register endpoint..." -ForegroundColor Yellow
Write-Host "Example PowerShell command:" -ForegroundColor Green
Write-Host 'Invoke-RestMethod -Uri "https://localhost:7000/api/auth/google-register" `' -ForegroundColor White
Write-Host '  -Method POST `' -ForegroundColor White
Write-Host '  -ContentType "application/json" `' -ForegroundColor White
Write-Host '  -Body (ConvertTo-Json @{' -ForegroundColor White
Write-Host '    googleToken = "YOUR_GOOGLE_ID_TOKEN_HERE"' -ForegroundColor White
Write-Host '    role = "SalonOwner"' -ForegroundColor White
Write-Host '    acceptTerms = $true' -ForegroundColor White
Write-Host '  }) `' -ForegroundColor White
Write-Host '  -SkipCertificateCheck' -ForegroundColor White

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "To get a Google ID token for testing:" -ForegroundColor Cyan
Write-Host "1. Navigate to: https://localhost:7000/debug-google.html" -ForegroundColor White
Write-Host "2. Sign in with Google and copy the ID token" -ForegroundColor White
Write-Host "3. Use that token in the PowerShell commands above" -ForegroundColor White
Write-Host ""
Write-Host "Current Google OAuth Configuration:" -ForegroundColor Cyan
Write-Host "- Client ID: 986486622148-0811nmnfmnjmnc0er554rvlqpn6dlvpl.apps.googleusercontent.com" -ForegroundColor White
Write-Host "- Project ID: stibe-booking-app" -ForegroundColor White
Write-Host "- Supported Platforms: Web, Android, iOS" -ForegroundColor White

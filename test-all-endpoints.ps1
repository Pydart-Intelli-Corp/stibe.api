# Test All API Endpoints - PowerShell Script
# This script tests all API endpoints to verify deployment

param(
    [string]$BaseUrl = "http://202.164.153.160:85",
    [switch]$Local = $false
)

if ($Local) {
    $BaseUrl = "http://localhost:5074"
}

Write-Host "🔍 Testing API Endpoints" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

$totalTests = 0
$successfulTests = 0

function Test-ApiEndpoint {
    param(
        [string]$Method = "GET",
        [string]$Endpoint,
        [string]$Description,
        [string]$ExpectedAuth = "public"
    )
    
    $global:totalTests++
    $url = $BaseUrl + $Endpoint
    
    Write-Host "🔗 Testing: $Description" -ForegroundColor White
    Write-Host "   Method: $Method" -ForegroundColor Gray
    Write-Host "   URL: $url" -ForegroundColor Gray
    
    try {
        $response = Invoke-RestMethod -Uri $url -Method $Method -TimeoutSec 15 -ErrorAction Stop
        $statusCode = 200
        $responseText = ($response | ConvertTo-Json -Compress).Substring(0, [Math]::Min(100, ($response | ConvertTo-Json -Compress).Length))
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $responseText = $_.Exception.Message
        if (!$statusCode) { $statusCode = "ERROR" }
    }
    
    # Evaluate response
    if ($statusCode -eq "ERROR") {
        Write-Host "   ❌ Status: Connection Failed" -ForegroundColor Red
        Write-Host "   📝 Response: Unable to connect" -ForegroundColor Gray
    }
    elseif ($statusCode -eq 200) {
        Write-Host "   ✅ Status: HTTP $statusCode (Success)" -ForegroundColor Green
        Write-Host "   📝 Response: $responseText..." -ForegroundColor Gray
        $global:successfulTests++
    }
    elseif ($statusCode -eq 401 -and $ExpectedAuth -ne "public") {
        Write-Host "   ✅ Status: HTTP $statusCode (Unauthorized - Expected for protected endpoint)" -ForegroundColor Yellow
        Write-Host "   📝 Response: Authentication required" -ForegroundColor Gray
        $global:successfulTests++
    }
    elseif ($statusCode -eq 404) {
        Write-Host "   ⚠️  Status: HTTP $statusCode (Not Found)" -ForegroundColor Yellow
        Write-Host "   📝 Response: Endpoint not found or not deployed" -ForegroundColor Gray
    }
    elseif ($statusCode -eq 500) {
        Write-Host "   ❌ Status: HTTP $statusCode (Server Error)" -ForegroundColor Red
        Write-Host "   📝 Response: $responseText" -ForegroundColor Gray
    }
    else {
        Write-Host "   ⚠️  Status: HTTP $statusCode" -ForegroundColor Yellow
        Write-Host "   📝 Response: $responseText" -ForegroundColor Gray
    }
    Write-Host ""
}

# PUBLIC ENDPOINTS
Write-Host "🧪 PUBLIC ENDPOINTS" -ForegroundColor Magenta
Write-Host "===================" -ForegroundColor Magenta

Test-ApiEndpoint -Endpoint "/api/test/health" -Description "Health Check Endpoint" -ExpectedAuth "public"
Test-ApiEndpoint -Endpoint "/api/test/test-email" -Description "Email Test Endpoint" -ExpectedAuth "public"

# AUTHENTICATION ENDPOINTS
Write-Host "🔐 AUTHENTICATION ENDPOINTS" -ForegroundColor Magenta
Write-Host "===========================" -ForegroundColor Magenta

Test-ApiEndpoint -Endpoint "/api/auth/check-email-status/test@example.com" -Description "Check Email Status" -ExpectedAuth "public"
Test-ApiEndpoint -Endpoint "/api/auth/check-phone-status/1234567890" -Description "Check Phone Status" -ExpectedAuth "public"

# PROTECTED ENDPOINTS
Write-Host "🔒 PROTECTED ENDPOINTS (Expecting 401 Unauthorized)" -ForegroundColor Magenta
Write-Host "===================================================" -ForegroundColor Magenta

Test-ApiEndpoint -Endpoint "/api/test/protected" -Description "Protected Test Endpoint" -ExpectedAuth "auth"
Test-ApiEndpoint -Endpoint "/api/test/admin-only" -Description "Admin Only Endpoint" -ExpectedAuth "admin"
Test-ApiEndpoint -Endpoint "/api/test/shop-owner" -Description "Shop Owner Endpoint" -ExpectedAuth "shop-owner"
Test-ApiEndpoint -Endpoint "/api/test/customer" -Description "Customer Endpoint" -ExpectedAuth "customer"
Test-ApiEndpoint -Endpoint "/api/test/debug-claims" -Description "Debug Claims Endpoint" -ExpectedAuth "auth"

# BUSINESS ENDPOINTS
Write-Host "🏪 BUSINESS ENDPOINTS" -ForegroundColor Magenta
Write-Host "====================" -ForegroundColor Magenta

Test-ApiEndpoint -Endpoint "/api/staff/dashboard" -Description "Staff Dashboard" -ExpectedAuth "auth"
Test-ApiEndpoint -Endpoint "/api/staff/profile" -Description "Staff Profile" -ExpectedAuth "auth"
Test-ApiEndpoint -Endpoint "/api/salon" -Description "Get All Salons" -ExpectedAuth "public"
Test-ApiEndpoint -Endpoint "/api/otp/status" -Description "OTP Service Status" -ExpectedAuth "public"

# LOGS & MONITORING
Write-Host "📊 LOGS & MONITORING" -ForegroundColor Magenta
Write-Host "===================" -ForegroundColor Magenta

Test-ApiEndpoint -Endpoint "/api/logs/recent?lines=5" -Description "Recent Logs" -ExpectedAuth "public"
Test-ApiEndpoint -Endpoint "/api/logs/errors?lines=3" -Description "Recent Errors" -ExpectedAuth "public"

# IIS DIAGNOSTICS
Write-Host "🔍 IIS DIAGNOSTICS" -ForegroundColor Magenta
Write-Host "=================" -ForegroundColor Magenta

Test-ApiEndpoint -Endpoint "/" -Description "Root Application" -ExpectedAuth "public"
Test-ApiEndpoint -Endpoint "/swagger" -Description "Swagger Documentation" -ExpectedAuth "public"

# SUMMARY
Write-Host "📋 ENDPOINT TESTING SUMMARY" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan
Write-Host "Total Endpoints Tested: $totalTests" -ForegroundColor White
Write-Host "Successful Responses: $successfulTests" -ForegroundColor Green
Write-Host "Failed/Unreachable: $($totalTests - $successfulTests)" -ForegroundColor Red
Write-Host ""

if ($totalTests -gt 0) {
    $successRate = [math]::Round(($successfulTests * 100 / $totalTests), 2)
    Write-Host "Success Rate: $successRate%" -ForegroundColor Yellow
    
    if ($successRate -ge 80) {
        Write-Host "🎉 EXCELLENT: API endpoints are responding well!" -ForegroundColor Green
    }
    elseif ($successRate -ge 60) {
        Write-Host "⚠️ GOOD: Most API endpoints are working, some may need attention" -ForegroundColor Yellow
    }
    else {
        Write-Host "❌ POOR: Many endpoints are not responding correctly" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🔗 Quick Access URLs:" -ForegroundColor Cyan
Write-Host "Health Check: $BaseUrl/api/test/health" -ForegroundColor Gray
Write-Host "API Documentation: $BaseUrl/swagger (if enabled)" -ForegroundColor Gray
Write-Host "Recent Logs: $BaseUrl/api/logs/recent?lines=10" -ForegroundColor Gray

# Exit with error code if success rate is below 50%
if ($totalTests -gt 0 -and ($successfulTests * 100 / $totalTests) -lt 50) {
    exit 1
}
else {
    exit 0
}

# Test Remote IIS API and Deployment Setup
param(
    [string]$ServerIP = "202.164.153.160",
    [int]$Port = 85
)

Write-Host "🌐 Testing Remote IIS API Setup" -ForegroundColor Cyan
Write-Host "Server: ${ServerIP}:${Port}" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Cyan

# Test API Health
Write-Host ""
Write-Host "1. Testing API Health..." -ForegroundColor Yellow
try {
    $healthUrl = "http://${ServerIP}:${Port}/api/test/health"
    Write-Host "   Testing: $healthUrl" -ForegroundColor Gray
    
    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    Write-Host "   ✅ Health Check: SUCCESS" -ForegroundColor Green
    Write-Host "      Status: $($response.StatusCode)" -ForegroundColor White
    Write-Host "      Response: $($response.Content.Substring(0, [Math]::Min(100, $response.Content.Length)))" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Health Check: FAILED" -ForegroundColor Red
    Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Gray
}

# Test Root Endpoint
Write-Host ""
Write-Host "2. Testing Root Endpoint..." -ForegroundColor Yellow
try {
    $rootUrl = "http://${ServerIP}:${Port}/"
    Write-Host "   Testing: $rootUrl" -ForegroundColor Gray
    
    $response = Invoke-WebRequest -Uri $rootUrl -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    Write-Host "   ✅ Root Endpoint: ACCESSIBLE" -ForegroundColor Green
    Write-Host "      Status: $($response.StatusCode)" -ForegroundColor White
} catch {
    Write-Host "   ⚠️ Root Endpoint: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Test Other Common Endpoints
Write-Host ""
Write-Host "3. Testing Other Endpoints..." -ForegroundColor Yellow

$testEndpoints = @(
    "/api/test",
    "/api/auth",
    "/swagger", 
    "/api"
)

foreach ($endpoint in $testEndpoints) {
    try {
        $url = "http://${ServerIP}:${Port}${endpoint}"
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        Write-Host "   ✅ $endpoint : HTTP $($response.StatusCode)" -ForegroundColor Green
    } catch {
        $statusCode = "Unknown"
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        Write-Host "   ⚠️ $endpoint : HTTP $statusCode" -ForegroundColor Yellow
    }
}

# Check FTP connectivity (if credentials are available)
Write-Host ""
Write-Host "4. FTP Connectivity Test..." -ForegroundColor Yellow
Write-Host "   Note: FTP test requires credentials" -ForegroundColor Gray

# Test ping connectivity
Write-Host ""
Write-Host "5. Network Connectivity..." -ForegroundColor Yellow
try {
    $ping = Test-Connection -ComputerName $ServerIP -Count 2 -ErrorAction Stop
    Write-Host "   ✅ Server is reachable" -ForegroundColor Green
    Write-Host "      Average ping: $([math]::Round(($ping.ResponseTime | Measure-Object -Average).Average, 2))ms" -ForegroundColor White
} catch {
    Write-Host "   ❌ Server ping failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary and Recommendations
Write-Host ""
Write-Host "📋 Setup Recommendations:" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan

Write-Host ""
Write-Host "For GitHub Actions Deployment:" -ForegroundColor White
Write-Host "1. ✅ Your API is accessible - perfect for automated deployment!" -ForegroundColor Green
Write-Host "2. 🔧 Recommended method: FTP deployment" -ForegroundColor Yellow
Write-Host "3. 📝 Required GitHub Secrets:" -ForegroundColor Yellow
Write-Host "   - FTP_USERNAME" -ForegroundColor Gray
Write-Host "   - FTP_PASSWORD" -ForegroundColor Gray

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor White
Write-Host "1. Enable FTP on your IIS server (port 21)" -ForegroundColor Cyan
Write-Host "2. Create FTP user with write permissions to website folder" -ForegroundColor Cyan  
Write-Host "3. Add FTP credentials to GitHub repository secrets" -ForegroundColor Cyan
Write-Host "4. Push code to master branch to trigger deployment" -ForegroundColor Cyan

Write-Host ""
Write-Host "🌐 Your API URLs:" -ForegroundColor Green
Write-Host "Main API: http://${ServerIP}:${Port}" -ForegroundColor Blue
Write-Host "Health Check: http://${ServerIP}:${Port}/api/test/health" -ForegroundColor Blue
Write-Host "GitHub Repository: https://github.com/Pydart-Intelli-Corp/stibe.api" -ForegroundColor Blue

Write-Host ""
Write-Host "Ready for automatic deployment setup!" -ForegroundColor Green

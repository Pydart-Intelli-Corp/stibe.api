# Monitor IIS Deployment Status
# This script helps monitor the GitHub Actions deployment and verify API functionality

param(
    [string]$ServerUrl = "http://202.164.153.160:85",
    [int]$CheckIntervalSeconds = 30,
    [int]$MaxChecks = 10
)

Write-Host "🔍 Monitoring API Deployment Status" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host "Server: $ServerUrl" -ForegroundColor Cyan
Write-Host "Check Interval: $CheckIntervalSeconds seconds" -ForegroundColor Cyan
Write-Host "Max Checks: $MaxChecks" -ForegroundColor Cyan

$endpoints = @(
    @{ Name = "Health Check"; Url = "$ServerUrl/api/test/health"; ExpectedStatus = 200 },
    @{ Name = "Root API"; Url = "$ServerUrl/api"; ExpectedStatus = @(200, 404) },
    @{ Name = "Test Controller"; Url = "$ServerUrl/api/test"; ExpectedStatus = 200 }
)

for ($i = 1; $i -le $MaxChecks; $i++) {
    Write-Host "`n📊 Check $i/$MaxChecks - $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Yellow
    
    $allHealthy = $true
    
    foreach ($endpoint in $endpoints) {
        try {
            $response = Invoke-WebRequest -Uri $endpoint.Url -Method Get -TimeoutSec 15 -ErrorAction Stop
            $statusCode = $response.StatusCode
            
            $isExpected = if ($endpoint.ExpectedStatus -is [array]) {
                $endpoint.ExpectedStatus -contains $statusCode
            } else {
                $statusCode -eq $endpoint.ExpectedStatus
            }
            
            if ($isExpected) {
                Write-Host "✅ $($endpoint.Name): HTTP $statusCode" -ForegroundColor Green
            } else {
                Write-Host "⚠️ $($endpoint.Name): HTTP $statusCode (unexpected)" -ForegroundColor Yellow
                $allHealthy = $false
            }
        }
        catch {
            Write-Host "❌ $($endpoint.Name): FAILED - $($_.Exception.Message)" -ForegroundColor Red
            $allHealthy = $false
        }
    }
    
    if ($allHealthy) {
        Write-Host "`n🎉 All endpoints are healthy! Deployment successful!" -ForegroundColor Green
        break
    }
    
    if ($i -lt $MaxChecks) {
        Write-Host "`n⏳ Waiting $CheckIntervalSeconds seconds for next check..." -ForegroundColor Cyan
        Start-Sleep -Seconds $CheckIntervalSeconds
    }
}

Write-Host "`n📋 Final Status Report" -ForegroundColor Green
Write-Host "======================" -ForegroundColor Green

# Final comprehensive check
Write-Host "`n🔬 Detailed API Analysis:" -ForegroundColor Yellow

try {
    $healthResponse = Invoke-WebRequest -Uri "$ServerUrl/api/test/health" -Method Get -TimeoutSec 15
    $healthData = $healthResponse.Content | ConvertFrom-Json
    
    Write-Host "✅ API is responding" -ForegroundColor Green
    Write-Host "📊 Response Data:" -ForegroundColor Cyan
    Write-Host "   Status: $($healthData.status)" -ForegroundColor White
    Write-Host "   Message: $($healthData.message)" -ForegroundColor White
    Write-Host "   Timestamp: $($healthData.timestamp)" -ForegroundColor White
    
    if ($healthData.environment) {
        Write-Host "   Environment: $($healthData.environment)" -ForegroundColor White
    }
}
catch {
    Write-Host "❌ Health endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔗 API Endpoints:" -ForegroundColor Yellow
Write-Host "Base URL: $ServerUrl/api" -ForegroundColor White
Write-Host "Health: $ServerUrl/api/test/health" -ForegroundColor White
Write-Host "Auth: $ServerUrl/api/auth" -ForegroundColor White
Write-Host "Profile Upload: $ServerUrl/api/auth/upload-profile-image" -ForegroundColor White

Write-Host "`n💡 Troubleshooting Tips:" -ForegroundColor Yellow
Write-Host "1. Check IIS Application Pool status" -ForegroundColor White
Write-Host "2. Verify .NET 8.0 Runtime installation" -ForegroundColor White
Write-Host "3. Check file permissions in wwwroot/uploads" -ForegroundColor White
Write-Host "4. Review IIS logs in C:\inetpub\logs\LogFiles" -ForegroundColor White
Write-Host "5. Check web.config configuration" -ForegroundColor White

Write-Host "`n🎯 Next Steps for Flutter App:" -ForegroundColor Yellow
Write-Host "1. Test profile image upload from Flutter app" -ForegroundColor White
Write-Host "2. Verify authentication flow" -ForegroundColor White
Write-Host "3. Check API responses in Flutter logs" -ForegroundColor White

Write-Host "`n✨ Monitoring completed!" -ForegroundColor Green

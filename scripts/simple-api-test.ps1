# Simple Remote API Test
$server = "202.164.153.160"
$port = "85"

Write-Host "Testing Remote API: $server`:$port" -ForegroundColor Cyan

# Test Health Endpoint
try {
    $url = "http://$server`:$port/test/api/test/health"
    Write-Host "Testing: $url" -ForegroundColor Yellow
    
    $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
    Write-Host "SUCCESS: HTTP $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Content: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Next Steps for Deployment:" -ForegroundColor Cyan
Write-Host "1. Set up FTP on your server" -ForegroundColor White
Write-Host "2. Add FTP credentials to GitHub Secrets" -ForegroundColor White
Write-Host "3. Push code to trigger deployment" -ForegroundColor White

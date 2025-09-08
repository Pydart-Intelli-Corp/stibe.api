# Remote IIS Deployment Setup Script

Write-Host "🌐 Setting up GitHub Actions for Remote IIS Deployment" -ForegroundColor Cyan
Write-Host "Remote Server: 202.164.153.160:85" -ForegroundColor Yellow
Write-Host "=================================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "✅ GitHub Actions workflows created:" -ForegroundColor Green
Write-Host "- deploy-to-iis.yml (FTP deployment - recommended)" -ForegroundColor White
Write-Host "- deploy-via-ftp.yml (Alternative FTP)" -ForegroundColor White  
Write-Host "- deploy-via-webdeploy.yml (Web Deploy method)" -ForegroundColor White
Write-Host "- deploy-to-remote-iis.yml (SSH method)" -ForegroundColor White

Write-Host ""
Write-Host "🔧 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Set up GitHub Repository Secrets" -ForegroundColor Cyan
Write-Host "   Go to: https://github.com/Pydart-Intelli-Corp/stibe.api/settings/secrets/actions" -ForegroundColor Blue

Write-Host ""
Write-Host "2. Add these secrets for FTP deployment:" -ForegroundColor Cyan
Write-Host "   Secret Name: FTP_USERNAME" -ForegroundColor White
Write-Host "   Secret Value: [Your FTP username for 202.164.153.160]" -ForegroundColor Gray
Write-Host ""
Write-Host "   Secret Name: FTP_PASSWORD" -ForegroundColor White  
Write-Host "   Secret Value: [Your FTP password for 202.164.153.160]" -ForegroundColor Gray

Write-Host ""
Write-Host "3. Test the deployment:" -ForegroundColor Cyan
Write-Host "   - Commit and push your changes to master branch" -ForegroundColor White
Write-Host "   - Monitor deployment: https://github.com/Pydart-Intelli-Corp/stibe.api/actions" -ForegroundColor Blue
Write-Host "   - Verify API: http://202.164.153.160:85/api/test/health" -ForegroundColor Blue

Write-Host ""
Write-Host "📋 Deployment Methods Available:" -ForegroundColor Yellow

Write-Host ""
Write-Host "🚀 FTP Deployment (Easiest):" -ForegroundColor Green
Write-Host "   ✅ Works with any FTP-enabled server" -ForegroundColor White
Write-Host "   ✅ No server-side setup required" -ForegroundColor White
Write-Host "   ✅ Uses GitHub hosted runners (free)" -ForegroundColor White
Write-Host "   ℹ️  Requires: FTP_USERNAME, FTP_PASSWORD secrets" -ForegroundColor Gray

Write-Host ""
Write-Host "🌐 Web Deploy (Professional):" -ForegroundColor Blue
Write-Host "   ✅ Most reliable for IIS" -ForegroundColor White
Write-Host "   ✅ Can handle app pool management" -ForegroundColor White
Write-Host "   ❗ Requires: Web Deploy installed on server" -ForegroundColor Yellow
Write-Host "   ℹ️  Requires: DEPLOY_USERNAME, DEPLOY_PASSWORD secrets" -ForegroundColor Gray

Write-Host ""
Write-Host "🔒 SSH Deployment (Advanced):" -ForegroundColor Magenta
Write-Host "   ✅ Most secure method" -ForegroundColor White
Write-Host "   ✅ Full server control" -ForegroundColor White
Write-Host "   ❗ Requires: SSH access to Windows server" -ForegroundColor Yellow
Write-Host "   ℹ️  Requires: REMOTE_USERNAME, REMOTE_PASSWORD secrets" -ForegroundColor Gray

Write-Host ""
Write-Host "💡 Recommendation:" -ForegroundColor Cyan
Write-Host "Start with FTP deployment - it's the simplest to set up!" -ForegroundColor Green

Write-Host ""
Write-Host "🔍 Current API Status:" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://202.164.153.160:85/api/test/health" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    Write-Host "✅ API is currently accessible" -ForegroundColor Green
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor White
} catch {
    Write-Host "⚠️  Could not reach API (this is normal if not running)" -ForegroundColor Yellow
    Write-Host "   URL: http://202.164.153.160:85/api/test/health" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🎉 Setup Complete!" -ForegroundColor Green
Write-Host "Add the FTP secrets to GitHub and you're ready to deploy!" -ForegroundColor Cyan

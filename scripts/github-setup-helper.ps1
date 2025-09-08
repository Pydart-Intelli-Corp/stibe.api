# GitHub Repository Secrets Setup Helper
# This script provides the exact values you need to add to GitHub Secrets

Write-Host "=== GitHub Secrets Setup for Stibe API ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "STEP 1: FTP Server Setup" -ForegroundColor Yellow
Write-Host "----------------------------------------"
Write-Host "1. Copy and run this script on your remote server (202.164.153.160):" -ForegroundColor White
Write-Host "   .\scripts\setup-ftp-server.ps1" -ForegroundColor Green
Write-Host ""

Write-Host "STEP 2: GitHub Secrets Configuration" -ForegroundColor Yellow
Write-Host "----------------------------------------"
Write-Host "Go to: https://github.com/Pydart-Intelli-Corp/stibe.api/settings/secrets/actions" -ForegroundColor White
Write-Host ""
Write-Host "Add these secrets:" -ForegroundColor White
Write-Host ""

Write-Host "Secret Name: " -NoNewline -ForegroundColor Cyan
Write-Host "FTP_USERNAME" -ForegroundColor Green
Write-Host "Secret Value: " -NoNewline -ForegroundColor Cyan
Write-Host "stibe-deploy" -ForegroundColor Green
Write-Host ""

Write-Host "Secret Name: " -NoNewline -ForegroundColor Cyan
Write-Host "FTP_PASSWORD" -ForegroundColor Green
Write-Host "Secret Value: " -NoNewline -ForegroundColor Cyan
Write-Host "StibeAPI2025!" -ForegroundColor Green
Write-Host ""

Write-Host "STEP 3: Test FTP Connection" -ForegroundColor Yellow
Write-Host "----------------------------------------"
Write-Host "After setting up FTP server, test the connection:" -ForegroundColor White
Write-Host ".\scripts\test-ftp-connection.ps1 -Username 'stibe-deploy' -Password 'StibeAPI2025!'" -ForegroundColor Green
Write-Host ""

Write-Host "STEP 4: Test Deployment" -ForegroundColor Yellow
Write-Host "----------------------------------------"
Write-Host "Push code to trigger deployment:" -ForegroundColor White
Write-Host "git add ." -ForegroundColor Green
Write-Host "git commit -m 'Test automatic deployment'" -ForegroundColor Green
Write-Host "git push origin master" -ForegroundColor Green
Write-Host ""

Write-Host "STEP 5: Verify Deployment" -ForegroundColor Yellow
Write-Host "----------------------------------------"
Write-Host "Check your API health:" -ForegroundColor White
Write-Host "http://202.164.153.160:85/test/api/test/health" -ForegroundColor Green
Write-Host ""

Write-Host "=== Security Notes ===" -ForegroundColor Red
Write-Host "• Change the default password to something more secure" -ForegroundColor Yellow
Write-Host "• Ensure FTP user has minimal required permissions" -ForegroundColor Yellow
Write-Host "• Consider setting up FTP over SSL/TLS" -ForegroundColor Yellow
Write-Host "• Monitor FTP access logs regularly" -ForegroundColor Yellow
Write-Host ""

Write-Host "=== Current Deployment Workflow ===" -ForegroundColor Cyan
Write-Host "✅ GitHub Actions workflow: .github/workflows/deploy-to-iis.yml" -ForegroundColor Green
Write-Host "✅ SSL certificate configuration: Fixed" -ForegroundColor Green
Write-Host "⏳ FTP server setup: Pending" -ForegroundColor Yellow
Write-Host "⏳ GitHub secrets: Pending" -ForegroundColor Yellow
Write-Host ""

Write-Host "Need help? Check the detailed guide:" -ForegroundColor White
Write-Host ".\docs\GITHUB_SECRETS_SETUP.md" -ForegroundColor Green

# GitHub Secrets Verification Helper
# Run this after adding secrets to verify everything is ready

Write-Host "=== GitHub Secrets Setup Verification ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ STEP 1: Add Repository Secrets" -ForegroundColor Green
Write-Host "Go to: https://github.com/Pydart-Intelli-Corp/stibe.api/settings/secrets/actions" -ForegroundColor White
Write-Host ""

Write-Host "Add these exact secrets:" -ForegroundColor Yellow
Write-Host "┌─────────────────────────────────────────┐"
Write-Host "│ Secret Name:  FTP_USERNAME              │"
Write-Host "│ Secret Value: test                      │"
Write-Host "├─────────────────────────────────────────┤"  
Write-Host "│ Secret Name:  FTP_PASSWORD              │"
Write-Host "│ Secret Value: Access`$404                │"
Write-Host "└─────────────────────────────────────────┘"
Write-Host ""

Write-Host "✅ STEP 2: Verify Workflow Configuration" -ForegroundColor Green
$workflowFile = ".github\workflows\deploy-to-iis.yml"
if (Test-Path $workflowFile) {
    Write-Host "✅ Workflow file exists: $workflowFile" -ForegroundColor Green
    
    $content = Get-Content $workflowFile -Raw
    if ($content -match 'FTP_PORT.*92') {
        Write-Host "✅ FTP Port 92 configured" -ForegroundColor Green
    } else {
        Write-Host "⚠️  FTP Port 92 not found in workflow" -ForegroundColor Yellow
    }
    
    if ($content -match '\$\{\{ secrets\.FTP_USERNAME \}\}') {
        Write-Host "✅ FTP_USERNAME secret reference found" -ForegroundColor Green
    } else {
        Write-Host "❌ FTP_USERNAME secret reference missing" -ForegroundColor Red
    }
    
    if ($content -match '\$\{\{ secrets\.FTP_PASSWORD \}\}') {
        Write-Host "✅ FTP_PASSWORD secret reference found" -ForegroundColor Green
    } else {
        Write-Host "❌ FTP_PASSWORD secret reference missing" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Workflow file not found: $workflowFile" -ForegroundColor Red
}
Write-Host ""

Write-Host "✅ STEP 3: Test Deployment" -ForegroundColor Green
Write-Host "After adding secrets, run these commands:" -ForegroundColor White
Write-Host "git add ." -ForegroundColor Cyan
Write-Host "git commit -m `"Test FTP deployment with custom port 92`"" -ForegroundColor Cyan
Write-Host "git push origin master" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ STEP 4: Monitor & Verify" -ForegroundColor Green
Write-Host "• GitHub Actions: https://github.com/Pydart-Intelli-Corp/stibe.api/actions" -ForegroundColor White
Write-Host "• API Health: http://202.164.153.160:85/test/api/test/health" -ForegroundColor White
Write-Host "• API Root: http://202.164.153.160:85/test/" -ForegroundColor White
Write-Host ""

Write-Host "🔧 Current Configuration Summary:" -ForegroundColor Yellow
Write-Host "• FTP Server: 202.164.153.160:92" -ForegroundColor White
Write-Host "• Website: 202.164.153.160:85" -ForegroundColor White
Write-Host "• Deploy Path: /test/" -ForegroundColor White
Write-Host "• Username: test" -ForegroundColor White
Write-Host "• Password: Access`$404" -ForegroundColor White
Write-Host ""

Write-Host "🚨 Security Reminder:" -ForegroundColor Red
Write-Host "• Never commit FTP credentials to code" -ForegroundColor Yellow
Write-Host "• Use GitHub Secrets for sensitive data" -ForegroundColor Yellow
Write-Host "• Monitor deployment logs for issues" -ForegroundColor Yellow
Write-Host ""

Write-Host "Need help? Check the detailed guide:" -ForegroundColor White
Write-Host "docs\GITHUB_SECRETS_STEP_BY_STEP.md" -ForegroundColor Green

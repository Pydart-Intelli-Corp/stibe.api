# Manual IIS Deployment Script for Stibe API
# Run this script if GitHub Actions deployment fails or for manual deployment

param(
    [string]$RemoteServer = "202.164.153.160",
    [string]$FtpPort = "92",
    [string]$FtpUsername = "",
    [string]$FtpPassword = "",
    [string]$LocalPublishPath = "E:\Published\stibe.api",
    [string]$RemotePath = "/test/"
)

Write-Host "🚀 Starting Manual IIS Deployment" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Step 1: Build and Publish
Write-Host "`n📦 Building and Publishing Application..." -ForegroundColor Yellow
try {
    Set-Location "E:\Stibe\stibe.api"
    
    Write-Host "Restoring packages..." -ForegroundColor Cyan
    dotnet restore
    
    Write-Host "Building application..." -ForegroundColor Cyan
    dotnet build --configuration Release --no-restore
    
    Write-Host "Publishing application..." -ForegroundColor Cyan
    dotnet publish --configuration Release --output $LocalPublishPath --no-restore
    
    Write-Host "✅ Build and Publish completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Build/Publish failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: FTP Upload (if credentials provided)
if ($FtpUsername -and $FtpPassword) {
    Write-Host "`n📤 Uploading files via FTP..." -ForegroundColor Yellow
    
    # Create FTP script file
    $ftpScript = @"
open $RemoteServer $FtpPort
$FtpUsername
$FtpPassword
cd $RemotePath
lcd $LocalPublishPath
binary
mput *
quit
"@
    
    $scriptPath = "$env:TEMP\ftp_deploy.txt"
    $ftpScript | Out-File -FilePath $scriptPath -Encoding ASCII
    
    try {
        ftp -s:$scriptPath
        Remove-Item $scriptPath -ErrorAction SilentlyContinue
        Write-Host "✅ FTP Upload completed!" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ FTP Upload failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "📝 Manual upload required to: ftp://${RemoteServer}:${FtpPort}${RemotePath}" -ForegroundColor Yellow
    }
}
else {
    Write-Host "`n⚠️ FTP credentials not provided. Files published to: $LocalPublishPath" -ForegroundColor Yellow
    Write-Host "📁 Manual upload required to: ftp://${RemoteServer}:${FtpPort}${RemotePath}" -ForegroundColor Yellow
}

# Step 3: Test deployment
Write-Host "`n🔍 Testing deployment..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

try {
    $healthUrl = "http://$RemoteServer:85/api/test/health"
    Write-Host "Testing health endpoint: $healthUrl" -ForegroundColor Cyan
    
    $response = Invoke-WebRequest -Uri $healthUrl -Method Get -TimeoutSec 30 -ErrorAction Stop
    
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API is responding correctly!" -ForegroundColor Green
        Write-Host "🌐 API Base URL: http://$RemoteServer:85" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ API responded with status code: $($response.StatusCode)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "🔧 Please check IIS configuration and application pool status" -ForegroundColor Yellow
}

Write-Host "`n📋 Deployment Summary" -ForegroundColor Green
Write-Host "=====================" -ForegroundColor Green
Write-Host "📦 Published to: $LocalPublishPath" -ForegroundColor White
Write-Host "🌐 Target Server: $RemoteServer:85" -ForegroundColor White
Write-Host "🔗 Health Check: http://$RemoteServer:85/api/test/health" -ForegroundColor White
Write-Host "`n💡 If deployment fails, check:" -ForegroundColor Yellow
Write-Host "   1. IIS Application Pool is running" -ForegroundColor White
Write-Host "   2. .NET 8.0 Runtime is installed" -ForegroundColor White
Write-Host "   3. File permissions on wwwroot/uploads" -ForegroundColor White
Write-Host "   4. web.config is properly configured" -ForegroundColor White

Write-Host "`n🎉 Deployment script completed!" -ForegroundColor Green

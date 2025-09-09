# PowerShell script to fix production uploads directory via remote deployment
# This script will create the uploads fix and deploy it

Write-Host "=== Stibe API - Production Uploads Fix ===" -ForegroundColor Green

# Set production deployment paths
$localPath = ".\production-uploads-fix"
$productionPath = "/test/"

Write-Host "Creating uploads fix deployment package..." -ForegroundColor Cyan

# Create local directory structure
New-Item -ItemType Directory -Path "$localPath\wwwroot\uploads\profile-images" -Force | Out-Null
New-Item -ItemType Directory -Path "$localPath\wwwroot\uploads\service-images" -Force | Out-Null
New-Item -ItemType Directory -Path "$localPath\wwwroot\uploads\shop-images" -Force | Out-Null
New-Item -ItemType Directory -Path "$localPath\wwwroot\uploads\product-images" -Force | Out-Null

# Create test files to ensure directories exist
"Test file for profile images" | Out-File "$localPath\wwwroot\uploads\profile-images\test.txt"
"Test file for service images" | Out-File "$localPath\wwwroot\uploads\service-images\test.txt"
"Test file for shop images" | Out-File "$localPath\wwwroot\uploads\shop-images\test.txt"
"Test file for product images" | Out-File "$localPath\wwwroot\uploads\product-images\test.txt"

# Create a simple index.html for the uploads directory
@"
<!DOCTYPE html>
<html>
<head>
    <title>Stibe Uploads Directory</title>
</head>
<body>
    <h1>Stibe API - Uploads Directory</h1>
    <p>This directory contains uploaded files for the Stibe application.</p>
    <p>If you can see this page, the uploads directory is properly configured.</p>
    <ul>
        <li><a href="profile-images/">Profile Images</a></li>
        <li><a href="service-images/">Service Images</a></li>
        <li><a href="shop-images/">Shop Images</a></li>
        <li><a href="product-images/">Product Images</a></li>
    </ul>
</body>
</html>
"@ | Out-File "$localPath\wwwroot\uploads\index.html"

# Create proper web.config for uploads directory
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <clear />
      <add name="StaticFileModuleHandler" path="*" verb="*" modules="StaticFileModule" resourceType="Either" requireAccess="Read" />
    </handlers>
    <staticContent>
      <mimeMap fileExtension=".jpg" mimeType="image/jpeg" />
      <mimeMap fileExtension=".jpeg" mimeType="image/jpeg" />
      <mimeMap fileExtension=".png" mimeType="image/png" />
      <mimeMap fileExtension=".gif" mimeType="image/gif" />
      <mimeMap fileExtension=".webp" mimeType="image/webp" />
      <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
    </staticContent>
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="52428800" />
      </requestFiltering>
    </security>
  </system.webServer>
</configuration>
"@ | Out-File "$localPath\wwwroot\uploads\web.config"

Write-Host "Created uploads directory structure:" -ForegroundColor Yellow
Get-ChildItem -Path "$localPath" -Recurse | ForEach-Object { Write-Host "  $($_.FullName)" -ForegroundColor Gray }

Write-Host "`nDeploying uploads fix to production..." -ForegroundColor Yellow
Write-Host "Note: This requires the same FTP credentials used in GitHub Actions" -ForegroundColor Cyan

# Test if we can connect to the production server
try {
    $response = Invoke-WebRequest -Uri "http://202.164.153.160:85/api/test/health" -Method GET -TimeoutSec 10
    Write-Host "✅ Production server is responding" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Cannot reach production server - deployment may fail" -ForegroundColor Yellow
}

Write-Host "`n📋 Manual Deployment Steps:" -ForegroundColor Cyan
Write-Host "1. Upload the contents of '$localPath' to the production server" -ForegroundColor White
Write-Host "2. Ensure the uploads directory has proper permissions (IIS_IUSRS: Full Control)" -ForegroundColor White
Write-Host "3. Restart the IIS application pool" -ForegroundColor White
Write-Host "4. Test uploads directory: http://202.164.153.160:85/uploads/" -ForegroundColor White

Write-Host "`n🚀 Quick Test Commands:" -ForegroundColor Cyan
Write-Host "Test uploads directory access:" -ForegroundColor White
Write-Host "  curl http://202.164.153.160:85/uploads/" -ForegroundColor Gray
Write-Host "Test profile upload endpoint:" -ForegroundColor White
Write-Host "  curl -X POST http://202.164.153.160:85/api/auth/profile/image -H 'Authorization: Bearer TOKEN' -F 'profileImage=@test.jpg'" -ForegroundColor Gray

Write-Host "`nUploads fix preparation complete!" -ForegroundColor Green

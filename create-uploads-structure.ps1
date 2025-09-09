# PowerShell script to fix production uploads directory
Write-Host "=== Stibe API - Production Uploads Fix ===" -ForegroundColor Green

# Set production deployment paths
$localPath = ".\production-uploads-fix"

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

Write-Host "Created uploads directory structure:" -ForegroundColor Yellow
Get-ChildItem -Path "$localPath" -Recurse | ForEach-Object { Write-Host "  $($_.FullName)" -ForegroundColor Gray }

# Test if we can connect to the production server
try {
    $response = Invoke-WebRequest -Uri "http://202.164.153.160:85/api/test/health" -Method GET -TimeoutSec 10
    Write-Host "Production server is responding" -ForegroundColor Green
} catch {
    Write-Host "Cannot reach production server - deployment may fail" -ForegroundColor Yellow
}

Write-Host "Uploads fix preparation complete!" -ForegroundColor Green

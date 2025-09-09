# PowerShell script to fix IIS upload issues for Stibe API
# Run this script on the IIS server to ensure proper configuration

Write-Host "=== Stibe API - IIS Upload Fix Script ===" -ForegroundColor Green

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges. Please run as Administrator." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Set the path to your deployed application
$AppPath = "C:\inetpub\wwwroot\stibe.api"  # Update this path as needed
$UploadsPath = "$AppPath\wwwroot\uploads"

Write-Host "Application Path: $AppPath" -ForegroundColor Cyan
Write-Host "Uploads Path: $UploadsPath" -ForegroundColor Cyan

# 1. Check if application directory exists
if (!(Test-Path $AppPath)) {
    Write-Host "ERROR: Application directory not found at $AppPath" -ForegroundColor Red
    Write-Host "Please update the AppPath variable in this script." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# 2. Create uploads directories if they don't exist
Write-Host "`n2. Creating upload directories..." -ForegroundColor Yellow
$directories = @(
    "$UploadsPath",
    "$UploadsPath\profile-images",
    "$UploadsPath\service-images", 
    "$UploadsPath\shop-images",
    "$UploadsPath\product-images"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force
        Write-Host "Created: $dir" -ForegroundColor Green
    } else {
        Write-Host "Exists: $dir" -ForegroundColor Gray
    }
}

# 3. Set proper permissions on uploads directory
Write-Host "`n3. Setting permissions on uploads directory..." -ForegroundColor Yellow
try {
    # Give IIS_IUSRS full control over uploads directory
    icacls $UploadsPath /grant "IIS_IUSRS:(OI)(CI)F" /T
    Write-Host "Set IIS_IUSRS permissions: OK" -ForegroundColor Green
    
    # Give application pool identity full control
    icacls $UploadsPath /grant "IIS AppPool\DefaultAppPool:(OI)(CI)F" /T
    Write-Host "Set DefaultAppPool permissions: OK" -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not set all permissions. Check manually." -ForegroundColor Yellow
}

# 4. Check web.config exists and has static file configuration
Write-Host "`n4. Checking web.config..." -ForegroundColor Yellow
$webConfigPath = "$AppPath\web.config"
if (Test-Path $webConfigPath) {
    $webConfigContent = Get-Content $webConfigPath -Raw
    if ($webConfigContent -like "*StaticFileModule*") {
        Write-Host "web.config has static file configuration: OK" -ForegroundColor Green
    } else {
        Write-Host "WARNING: web.config may be missing static file configuration" -ForegroundColor Yellow
    }
} else {
    Write-Host "ERROR: web.config not found" -ForegroundColor Red
}

# 5. Test file creation in uploads directory
Write-Host "`n5. Testing file write permissions..." -ForegroundColor Yellow
$testFile = "$UploadsPath\test.txt"
try {
    "Test file" | Out-File -FilePath $testFile -Force
    if (Test-Path $testFile) {
        Remove-Item $testFile -Force
        Write-Host "File write test: OK" -ForegroundColor Green
    }
} catch {
    Write-Host "ERROR: Cannot write files to uploads directory" -ForegroundColor Red
    Write-Host "Check permissions and disk space" -ForegroundColor Yellow
}

# 6. Check IIS features
Write-Host "`n6. Checking IIS features..." -ForegroundColor Yellow
$features = @(
    "IIS-WebServerRole",
    "IIS-WebServer", 
    "IIS-CommonHttpFeatures",
    "IIS-StaticContent",
    "IIS-NetFxExtensibility45",
    "IIS-AspNetCoreModule",
    "IIS-AspNetCoreModuleV2"
)

foreach ($feature in $features) {
    $featureState = Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue
    if ($featureState -and $featureState.State -eq "Enabled") {
        Write-Host "$feature: Enabled" -ForegroundColor Green
    } else {
        Write-Host "$feature: NOT ENABLED" -ForegroundColor Yellow
    }
}

# 7. Restart IIS
Write-Host "`n7. Restarting IIS..." -ForegroundColor Yellow
try {
    iisreset
    Write-Host "IIS restarted successfully" -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not restart IIS automatically" -ForegroundColor Yellow
}

Write-Host "`n=== Fix Script Complete ===" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Test uploading a profile image through your API" -ForegroundColor White
Write-Host "2. Check the uploads directory for created files" -ForegroundColor White
Write-Host "3. Test accessing uploaded files via browser (e.g., http://yourserver/uploads/profile-images/filename.jpg)" -ForegroundColor White
Write-Host "4. Check application logs if issues persist" -ForegroundColor White

Read-Host "`nPress Enter to exit"

# IIS Setup Script for Stibe API
# Run this script as Administrator to set up IIS for your API

# Enable IIS features
Write-Host "Enabling IIS features..." -ForegroundColor Green
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-HttpErrors, IIS-HttpLogging, IIS-RequestFiltering, IIS-StaticContent, IIS-DefaultDocument, IIS-DirectoryBrowsing, IIS-ASPNET45, IIS-NetFxExtensibility45, IIS-ISAPIExtensions, IIS-ISAPIFilter, IIS-HttpCompressionStatic, IIS-ManagementConsole

# Import WebAdministration module
Import-Module WebAdministration

# Variables
$siteName = "StibeAPI"
$appPoolName = "StibeAPI"
$physicalPath = "C:\inetpub\wwwroot\StibeAPI"
$port = 80

# Create directory if it doesn't exist
if (-not (Test-Path $physicalPath)) {
    New-Item -ItemType Directory -Force -Path $physicalPath
    Write-Host "Created directory: $physicalPath" -ForegroundColor Green
}

# Create Application Pool
if (Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue) {
    Write-Host "Application Pool '$appPoolName' already exists" -ForegroundColor Yellow
} else {
    New-WebAppPool -Name $appPoolName
    Write-Host "Created Application Pool: $appPoolName" -ForegroundColor Green
}

# Configure Application Pool
Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""  # For .NET Core
Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "enable32BitAppOnWin64" -Value $false

# Create Website
if (Get-Website -Name $siteName -ErrorAction SilentlyContinue) {
    Write-Host "Website '$siteName' already exists" -ForegroundColor Yellow
} else {
    New-Website -Name $siteName -Port $port -PhysicalPath $physicalPath -ApplicationPool $appPoolName
    Write-Host "Created Website: $siteName" -ForegroundColor Green
}

# Set permissions
$acl = Get-Acl $physicalPath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IUSR", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl $physicalPath $acl

Write-Host "IIS setup completed!" -ForegroundColor Green
Write-Host "Site URL: http://localhost:$port" -ForegroundColor Cyan
Write-Host "Physical Path: $physicalPath" -ForegroundColor Cyan

# Install .NET Core Hosting Bundle (if not already installed)
Write-Host "Make sure .NET 8.0 Hosting Bundle is installed!" -ForegroundColor Yellow
Write-Host "Download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow

# FTP Server Setup Script for Remote IIS Server
# Run this script on your remote server (202.164.153.160) as Administrator

Write-Host "Setting up FTP Server for Stibe API Deployment..." -ForegroundColor Green

# 1. Install IIS FTP Service
Write-Host "Installing IIS FTP Service..." -ForegroundColor Yellow
Enable-WindowsOptionalFeature -Online -FeatureName IIS-FTPServer -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-FTPSvc -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-FTPExtensibility -All

# 2. Import WebAdministration module
Import-Module WebAdministration

# 3. Create FTP Site
$ftpSiteName = "StibeAPI-FTP"
$ftpPort = 21
$physicalPath = "C:\inetpub\wwwroot\test"

Write-Host "Creating FTP Site: $ftpSiteName" -ForegroundColor Yellow

# Remove existing FTP site if it exists
if (Get-WebSite -Name $ftpSiteName -ErrorAction SilentlyContinue) {
    Remove-WebSite -Name $ftpSiteName
}

# Create new FTP site
New-WebFtpSite -Name $ftpSiteName -PhysicalPath $physicalPath -Port $ftpPort

# 4. Configure FTP Authentication
Write-Host "Configuring FTP Authentication..." -ForegroundColor Yellow

# Enable Basic Authentication
Set-WebConfiguration -Filter "/system.ftpServer/security/authentication/basicAuthentication" -Value @{enabled="true"} -PSPath "IIS:" -Location "$ftpSiteName"

# Disable Anonymous Authentication
Set-WebConfiguration -Filter "/system.ftpServer/security/authentication/anonymousAuthentication" -Value @{enabled="false"} -PSPath "IIS:" -Location "$ftpSiteName"

# 5. Create FTP User
$ftpUsername = "stibe-deploy"
$ftpPassword = "StibeAPI2025!" # Change this to a secure password

Write-Host "Creating FTP User: $ftpUsername" -ForegroundColor Yellow

# Create local user
try {
    $securePassword = ConvertTo-SecureString $ftpPassword -AsPlainText -Force
    New-LocalUser -Name $ftpUsername -Password $securePassword -Description "FTP user for Stibe API deployment" -PasswordNeverExpires
    Add-LocalGroupMember -Group "IIS_IUSRS" -Member $ftpUsername
} catch {
    Write-Host "User might already exist. Updating password..." -ForegroundColor Yellow
    Set-LocalUser -Name $ftpUsername -Password $securePassword
}

# 6. Configure FTP Authorization
Write-Host "Configuring FTP Authorization..." -ForegroundColor Yellow

# Allow the FTP user to read and write
Add-WebConfiguration -Filter "/system.ftpServer/security/authorization" -Value @{accessType="Allow"; users=$ftpUsername; permissions="Read,Write"} -PSPath "IIS:" -Location "$ftpSiteName"

# 7. Configure FTP SSL (Optional but recommended)
Write-Host "Configuring FTP SSL..." -ForegroundColor Yellow
Set-WebConfiguration -Filter "/system.ftpServer/security/ssl" -Value @{controlChannelPolicy="SslAllow"; dataChannelPolicy="SslAllow"} -PSPath "IIS:" -Location "$ftpSiteName"

# 8. Configure FTP Firewall Support
Write-Host "Configuring FTP Firewall Support..." -ForegroundColor Yellow
Set-WebConfiguration -Filter "/system.ftpServer/firewallSupport" -Value @{externalIp4Address="202.164.153.160"} -PSPath "IIS:"

# 9. Set directory permissions
Write-Host "Setting directory permissions..." -ForegroundColor Yellow
$acl = Get-Acl $physicalPath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($ftpUsername, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path $physicalPath -AclObject $acl

# 10. Restart FTP Service
Write-Host "Restarting FTP Service..." -ForegroundColor Yellow
Restart-Service FTPSVC

Write-Host "FTP Server Setup Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "FTP Connection Details:" -ForegroundColor Cyan
Write-Host "Server: 202.164.153.160" -ForegroundColor White
Write-Host "Port: 21" -ForegroundColor White
Write-Host "Username: $ftpUsername" -ForegroundColor White
Write-Host "Password: $ftpPassword" -ForegroundColor White
Write-Host "Directory: $physicalPath" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANT: Save these credentials securely and add them to GitHub Secrets!" -ForegroundColor Red
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test FTP connection from your local machine"
Write-Host "2. Add FTP_USERNAME and FTP_PASSWORD to GitHub repository secrets"
Write-Host "3. Push code to master branch to trigger deployment"

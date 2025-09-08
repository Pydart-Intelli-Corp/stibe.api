# FTP Diagnosis Script for Remote Server
# Run this script on your remote server (202.164.153.160) to diagnose FTP issues

Write-Host "=== FTP Server Diagnosis for Error 530 ===" -ForegroundColor Cyan
Write-Host ""

# Check FTP Service
Write-Host "1. Checking FTP Service Status..." -ForegroundColor Yellow
$ftpService = Get-Service -Name "FTPSVC" -ErrorAction SilentlyContinue
if ($ftpService) {
    Write-Host "   FTP Service Status: $($ftpService.Status)" -ForegroundColor $(if($ftpService.Status -eq 'Running') { 'Green' } else { 'Red' })
    if ($ftpService.Status -ne 'Running') {
        Write-Host "   ⚠️ Starting FTP Service..." -ForegroundColor Yellow
        Start-Service FTPSVC
    }
} else {
    Write-Host "   ❌ FTP Service not found - install FTP Server role" -ForegroundColor Red
}
Write-Host ""

# Check User Account
Write-Host "2. Checking User Account 'test'..." -ForegroundColor Yellow
try {
    $user = Get-LocalUser -Name "test" -ErrorAction Stop
    Write-Host "   ✅ User 'test' exists" -ForegroundColor Green
    Write-Host "   Enabled: $($user.Enabled)" -ForegroundColor $(if($user.Enabled) { 'Green' } else { 'Red' })
    Write-Host "   Password Expires: $($user.PasswordExpires)" -ForegroundColor White
    Write-Host "   Last Logon: $($user.LastLogon)" -ForegroundColor White
} catch {
    Write-Host "   ❌ User 'test' not found" -ForegroundColor Red
    Write-Host "   Creating user 'test'..." -ForegroundColor Yellow
    try {
        $securePassword = ConvertTo-SecureString "Access`$404" -AsPlainText -Force
        New-LocalUser -Name "test" -Password $securePassword -Description "FTP Deploy User" -PasswordNeverExpires
        Add-LocalGroupMember -Group "IIS_IUSRS" -Member "test"
        Write-Host "   ✅ User 'test' created" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Failed to create user: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""

# Check Target Directory
Write-Host "3. Checking Target Directory..." -ForegroundColor Yellow
$targetDir = "C:\inetpub\wwwroot\test"
if (Test-Path $targetDir) {
    Write-Host "   ✅ Directory exists: $targetDir" -ForegroundColor Green
    
    # Check permissions
    try {
        $acl = Get-Acl $targetDir
        $testUserAccess = $acl.Access | Where-Object { $_.IdentityReference -like "*test*" }
        if ($testUserAccess) {
            Write-Host "   ✅ User 'test' has permissions: $($testUserAccess.FileSystemRights)" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️ User 'test' has no explicit permissions" -ForegroundColor Yellow
            Write-Host "   Setting Full Control permissions..." -ForegroundColor Yellow
            
            $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("test", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
            $acl.SetAccessRule($accessRule)
            Set-Acl -Path $targetDir -AclObject $acl
            Write-Host "   ✅ Permissions set for user 'test'" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ❌ Error checking permissions: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ❌ Directory does not exist: $targetDir" -ForegroundColor Red
    Write-Host "   Creating directory..." -ForegroundColor Yellow
    try {
        New-Item -Path $targetDir -ItemType Directory -Force
        Write-Host "   ✅ Directory created" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Failed to create directory: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""

# Check IIS FTP Site
Write-Host "4. Checking IIS FTP Configuration..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    
    $ftpSites = Get-WebSite | Where-Object { $_.Bindings.Collection.Protocol -eq "ftp" }
    if ($ftpSites) {
        foreach ($site in $ftpSites) {
            Write-Host "   FTP Site: $($site.Name)" -ForegroundColor Green
            Write-Host "   Physical Path: $($site.PhysicalPath)" -ForegroundColor White
            Write-Host "   State: $($site.State)" -ForegroundColor $(if($site.State -eq 'Started') { 'Green' } else { 'Red' })
            
            # Check FTP port binding
            $ftpBinding = $site.Bindings.Collection | Where-Object { $_.Protocol -eq "ftp" }
            if ($ftpBinding) {
                Write-Host "   FTP Port: $($ftpBinding.bindingInformation)" -ForegroundColor White
            }
        }
    } else {
        Write-Host "   ⚠️ No FTP sites found in IIS" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Error checking IIS: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test Local FTP Connection
Write-Host "5. Testing Local FTP Connection..." -ForegroundColor Yellow
try {
    $ftpUri = "ftp://localhost:92/"
    $request = [System.Net.FtpWebRequest]::Create($ftpUri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $request.Credentials = New-Object System.Net.NetworkCredential("test", "Access`$404")
    $request.Timeout = 10000
    
    $response = $request.GetResponse()
    Write-Host "   ✅ FTP connection successful!" -ForegroundColor Green
    $response.Close()
} catch {
    Write-Host "   ❌ FTP connection failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Message -like "*530*") {
        Write-Host "   🔧 Error 530 detected - authentication/directory issue" -ForegroundColor Yellow
    }
}
Write-Host ""

# Recommendations
Write-Host "=== RECOMMENDATIONS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If FTP connection failed with Error 530:" -ForegroundColor Yellow
Write-Host "1. Open IIS Manager" -ForegroundColor White
Write-Host "2. Select your FTP site → FTP Authorization Rules" -ForegroundColor White
Write-Host "3. Add Allow Rule for user 'test' with Read+Write permissions" -ForegroundColor White
Write-Host "4. Restart FTP service: Restart-Service FTPSVC" -ForegroundColor White
Write-Host ""
Write-Host "Test commands to run after fixes:" -ForegroundColor Yellow
Write-Host "ftp localhost 92" -ForegroundColor Green
Write-Host "# Login: test / Access`$404" -ForegroundColor Gray
Write-Host ""
Write-Host "If still failing, try creating a new FTP user:" -ForegroundColor Yellow
Write-Host "net user ftpdeploy Access`$404 /add" -ForegroundColor Green
Write-Host "net localgroup `"IIS_IUSRS`" ftpdeploy /add" -ForegroundColor Green

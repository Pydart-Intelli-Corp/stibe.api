# GitHub Actions Deployment to Existing IIS Website

## Overview

Your Stibe API is already set up as a proper IIS website on server `202.164.153.160:85`. This guide will help you set up automatic deployment from GitHub to your existing IIS configuration.

## Your Current IIS Setup ✅

Since you've manually configured your IIS server with:
- ✅ Website created in IIS Manager
- ✅ Application Pool configured  
- ✅ Port 88 binding set up
- ✅ Physical directory structure
- ✅ .NET runtime installed

You just need to enable automatic file deployment from GitHub!

## Quick Setup (5 minutes)

### Step 1: Enable FTP on Your IIS Server

On your remote server (`202.164.153.160`):

1. **Open IIS Manager**
2. **Install FTP Publishing Service** (if not already installed):
   ```
   Server Manager → Add Roles and Features → Web Server (IIS) → FTP Server
   ```

3. **Configure FTP Site**:
   - Right-click your website → Add FTP Publishing
   - IP Address: All Unassigned
   - Port: 21 (or custom port)
   - SSL: No SSL (or configure if needed)
   - Authentication: Basic
   - Allow access to: Specific users
   - Permissions: Read + Write

### Step 2: Create FTP User Account

On your server:
```cmd
# Create a user account for deployment
net user deployuser [password] /add
net localgroup IIS_IUSRS deployuser /add
```

Or use an existing Windows account with appropriate permissions.

### Step 3: Set Up GitHub Secrets

1. Go to your repository: https://github.com/Pydart-Intelli-Corp/stibe.api
2. Navigate to: **Settings** → **Secrets and variables** → **Actions**
3. Add these repository secrets:

```
Name: FTP_USERNAME
Value: [your FTP username - e.g., deployuser]

Name: FTP_PASSWORD  
Value: [your FTP password]
```

### Step 4: Test Deployment

1. **Commit and push** any change to your `master` branch
2. **Monitor deployment**: https://github.com/Pydart-Intelli-Corp/stibe.api/actions
3. **Verify API**: http://202.164.153.160:85/api/test/health

## Alternative Setup Methods

### Option A: Using Windows File Share (Network Drive)

If FTP is not available, you can use file share:

1. **Share your website folder** on the server
2. **Use a different GitHub Action** for file copy:

```yaml
- name: Deploy via File Share
  run: |
    # Mount network drive
    net use Z: \\202.164.153.160\WebsiteShare /user:deployuser password
    
    # Copy files
    robocopy ./publish Z:\ /E /R:3 /W:5
    
    # Unmount
    net use Z: /delete
```

### Option B: Using PowerShell Remoting (Most Advanced)

If you have PowerShell remoting enabled:

```yaml
- name: Deploy via PowerShell Remoting
  run: |
    # Connect and deploy
    $session = New-PSSession -ComputerName 202.164.153.160 -Credential $cred
    Copy-Item ./publish/* -Destination C:\inetpub\wwwroot\YourSite -ToSession $session -Recurse -Force
```

## What the Workflow Does

1. ✅ **Builds** your .NET 8.0 application
2. ✅ **Publishes** release version
3. ✅ **Uploads** files via FTP to your IIS website directory
4. ✅ **Tests** your API endpoints after deployment
5. ✅ **Reports** deployment status

## Troubleshooting

### Common Issues:

**FTP Connection Failed:**
- Check Windows Firewall (port 21)
- Verify FTP service is running
- Ensure user has proper permissions

**Files uploaded but site not working:**
- Check Application Pool is running
- Verify .NET 8.0 Runtime is installed
- Check application pool identity permissions

**Health check fails:**
- Manually test: http://202.164.153.160:85/api/test/health
- Check IIS logs: `C:\inetpub\logs\LogFiles\W3SVC1\`
- Verify database connection string

### IIS Application Pool Settings

Ensure your app pool is configured correctly:
- **.NET CLR Version**: No Managed Code (for .NET Core/8.0)
- **Managed Pipeline Mode**: Integrated
- **Identity**: ApplicationPoolIdentity
- **Process Model → Idle Timeout**: 20 minutes (or 0 for always running)

### Permissions Check

Your website directory should have these permissions:
```cmd
icacls "C:\path\to\your\website" /grant "IIS_IUSRS:(OI)(CI)F"
icacls "C:\path\to\your\website" /grant "IUSR:(OI)(CI)RX"
```

## Testing Your Setup

Run this PowerShell script on your development machine to test connectivity:

```powershell
# Test API connectivity
$response = Invoke-WebRequest "http://202.164.153.160:85/api/test/health"
Write-Host "Status: $($response.StatusCode)"
Write-Host "Content: $($response.Content)"

# Test FTP connectivity (replace with your credentials)
$ftpClient = [System.Net.FtpWebRequest]::Create("ftp://202.164.153.160/")
$ftpClient.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
# Add credentials and test
```

## Next Steps After Setup

Once deployment is working:
1. **Set up staging environment** (optional)
2. **Configure database migrations** in the workflow
3. **Add email notifications** for deployment status
4. **Set up monitoring** for your API

---

**Your API will be automatically deployed every time you push to the master branch!** 🚀

Need help with any step? Check the GitHub Actions logs for detailed error messages.

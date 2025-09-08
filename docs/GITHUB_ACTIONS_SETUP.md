# GitHub Actions Self-Hosted Runner Setup Guide

## Prerequisites

1. **Windows Machine** with Administrator privileges
2. **IIS** installed with ASP.NET Core hosting support
3. **.NET 8.0 SDK** installed
4. **Git** installed
5. **PowerShell 5.1 or later**

## Step 1: Install and Configure IIS

Run PowerShell as Administrator and execute:

```powershell
# Navigate to your project directory
cd "D:\MY PROJECTS\Stibe\stibe.api"

# Run the IIS setup script
.\scripts\setup-iis.ps1
```

## Step 2: Install .NET 8.0 Hosting Bundle

1. Download the ASP.NET Core Runtime (latest version) from:
   https://dotnet.microsoft.com/download/dotnet/8.0

2. Look for "Hosting Bundle" and install it
3. Restart IIS after installation:
   ```powershell
   iisreset
   ```

## Step 3: Set up GitHub Self-Hosted Runner

### 3.1 Download and Configure Runner

1. Go to your GitHub repository: `https://github.com/Pydart-Intelli-Corp/stibe.api`
2. Navigate to: **Settings** → **Actions** → **Runners**
3. Click **"New self-hosted runner"**
4. Select **Windows** and **x64**
5. Follow the download instructions, or run these commands in PowerShell as Administrator:

```powershell
# Create runner directory
mkdir C:\actions-runner
cd C:\actions-runner

# Download runner (use the URL from GitHub)
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v2.311.0/actions-runner-win-x64-2.311.0.zip -OutFile actions-runner-win-x64.zip

# Extract
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64.zip", "$PWD")
```

### 3.2 Configure the Runner

Run the configuration command provided by GitHub (it will look like this):

```powershell
.\config.cmd --url https://github.com/Pydart-Intelli-Corp/stibe.api --token YOUR_TOKEN_HERE
```

When prompted:
- **Enter the name of runner group**: Press Enter (default)
- **Enter the name of runner**: `stibe-local-runner` (or any name you prefer)
- **Enter any additional labels**: `iis,local,windows` (optional)
- **Enter name of work folder**: Press Enter (default)

### 3.3 Install Runner as Windows Service

```powershell
# Install as service (run as Administrator)
.\svc.sh install

# Start the service
.\svc.sh start

# Check service status
Get-Service actions.runner.*
```

## Step 4: Configure Repository Secrets (Optional)

If you need to store sensitive configuration:

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. Add any secrets your application might need (database passwords, API keys, etc.)

## Step 5: Test the Setup

1. **Manual Test**: Run the deployment script manually
   ```powershell
   cd "D:\MY PROJECTS\Stibe\stibe.api"
   .\scripts\deploy.ps1
   ```

2. **GitHub Actions Test**: 
   - Make a small change to your code
   - Commit and push to the `master` branch
   - Check the Actions tab in your GitHub repository
   - Monitor the deployment process

## Step 6: Configure Firewall (if needed)

If you need to access your API from other machines:

```powershell
# Allow HTTP traffic
New-NetFirewallRule -DisplayName "Allow HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow

# Allow HTTPS traffic (if using HTTPS)
New-NetFirewallRule -DisplayName "Allow HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

## Troubleshooting

### Common Issues:

1. **Runner not appearing online**:
   ```powershell
   # Check service status
   Get-Service actions.runner.*
   
   # Restart service
   .\svc.sh stop
   .\svc.sh start
   ```

2. **IIS permission issues**:
   ```powershell
   # Reset IIS permissions
   cd "C:\inetpub\wwwroot\StibeAPI"
   icacls . /grant "IIS_IUSRS:(OI)(CI)F" /T
   icacls . /grant "IUSR:(OI)(CI)RX" /T
   ```

3. **App Pool keeps stopping**:
   ```powershell
   # Check Windows Event Log
   Get-EventLog -LogName Application -Source "Application Error" -Newest 10
   
   # Check IIS logs
   Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" | Select-Object -Last 20
   ```

4. **Deploy fails with permission errors**:
   - Ensure the runner service is running as a user with appropriate permissions
   - Consider running the runner service as LocalSystem or a dedicated service account

### Logs and Monitoring:

- **GitHub Actions logs**: Available in your repository's Actions tab
- **IIS logs**: `C:\inetpub\logs\LogFiles\W3SVC1\`
- **Windows Event Log**: Application and System logs
- **Runner logs**: `C:\actions-runner\_diag\` folder

## Security Considerations:

1. **Keep your runner machine secure** - it has access to your code and deployment environment
2. **Use HTTPS** for production deployments
3. **Regularly update** the runner software
4. **Monitor access logs** for suspicious activity
5. **Consider using a dedicated service account** for the runner

## Maintenance:

1. **Update runner**: GitHub will notify you when updates are available
2. **Monitor disk space**: Backups and logs can accumulate
3. **Review deployment logs** regularly
4. **Test recovery procedures** periodically

---

## Quick Commands Reference:

```powershell
# Check runner status
Get-Service actions.runner.*

# Restart IIS
iisreset

# Check app pool status
Get-IISAppPool

# Manual deployment
cd "D:\MY PROJECTS\Stibe\stibe.api"
.\scripts\deploy.ps1

# Check recent backups
Get-ChildItem "C:\inetpub\backups" | Sort-Object CreationTime -Descending | Select-Object -First 5
```

# Deployment Scripts

This folder contains PowerShell scripts for setting up and managing GitHub Actions deployment to local IIS.

## Scripts Overview

### 🚀 `quick-start.ps1`
**Main setup script - Start here!**
- Runs all setup steps in order
- Validates your environment
- Provides instructions for GitHub runner setup

**Usage:**
```powershell
# Run as Administrator
.\scripts\quick-start.ps1
```

### 🏗️ `setup-iis.ps1`
Sets up IIS with the required configuration for the Stibe API
- Creates application pool
- Creates website
- Sets permissions
- Configures for .NET Core

### ✅ `validate-setup.ps1`
Validates your environment is ready for deployment
- Checks IIS installation
- Verifies .NET SDK
- Tests project compilation
- Checks GitHub runner status

### 📦 `deploy.ps1`
Manual deployment script (alternative to GitHub Actions)
- Pulls latest code
- Builds and publishes
- Deploys to IIS
- Includes backup and rollback

## Quick Setup Guide

1. **Run as Administrator:**
   ```powershell
   cd "D:\MY PROJECTS\Stibe\stibe.api"
   .\scripts\quick-start.ps1
   ```

2. **Set up GitHub Runner:**
   - Go to: https://github.com/Pydart-Intelli-Corp/stibe.api/settings/actions/runners
   - Click "New self-hosted runner"
   - Follow the instructions

3. **Test Deployment:**
   - Make a commit and push to master branch
   - Check GitHub Actions tab
   - Visit: http://localhost/StibeAPI/api/test/health

## Troubleshooting

### Common Issues:

**Runner not starting:**
```powershell
cd C:\actions-runner
.\svc.sh stop
.\svc.sh start
```

**Permission errors:**
```powershell
# Reset IIS permissions
icacls "C:\inetpub\wwwroot\StibeAPI" /grant "IIS_IUSRS:(OI)(CI)F" /T
```

**App pool stopping:**
```powershell
# Check event logs
Get-EventLog -LogName Application -Source "Application Error" -Newest 5
```

### Manual Deployment:
If GitHub Actions fails, you can deploy manually:
```powershell
.\scripts\deploy.ps1
```

### Environment Validation:
Check your setup anytime:
```powershell
.\scripts\validate-setup.ps1
```

## Files Structure After Setup:

```
stibe.api/
├── .github/workflows/
│   └── deploy-to-iis.yml          # GitHub Actions workflow
├── scripts/
│   ├── quick-start.ps1            # Main setup script
│   ├── setup-iis.ps1              # IIS configuration
│   ├── validate-setup.ps1         # Environment validation
│   └── deploy.ps1                 # Manual deployment
└── docs/
    └── GITHUB_ACTIONS_SETUP.md    # Detailed documentation
```

## Security Notes:

- Scripts must run as Administrator for IIS configuration
- Runner service should use appropriate service account
- Monitor deployment logs for security issues
- Keep runner software updated

---

**Need help?** Check `docs/GITHUB_ACTIONS_SETUP.md` for detailed instructions.

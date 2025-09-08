# GitHub Actions - Remote IIS Server Deployment Guide

## Overview

Your API is hosted on a remote IIS server at `http://202.164.153.160:85`. I've created three different deployment approaches for you to choose from, depending on what access you have to your remote server.

## Deployment Options

### 🚀 **Option 1: FTP Deployment** (Recommended - Easiest)

**Requirements:**
- FTP access to your remote server
- FTP credentials

**Steps:**
1. **Set up GitHub Secrets:**
   - Go to: https://github.com/Pydart-Intelli-Corp/stibe.api/settings/secrets/actions
   - Add these secrets:
     - `FTP_USERNAME`: Your FTP username
     - `FTP_PASSWORD`: Your FTP password

2. **Enable the workflow:**
   - Rename `.github/workflows/deploy-via-ftp.yml` to be the active workflow
   - Or copy its contents to your existing workflow file

3. **Test deployment:**
   - Push changes to master branch
   - Monitor at: https://github.com/Pydart-Intelli-Corp/stibe.api/actions

---

### 🌐 **Option 2: Web Deploy (MSDeploy)** (Most Professional)

**Requirements:**
- Web Deploy installed on remote server
- Web Management Service running
- Deploy user account

**Setup on Remote Server:**
```powershell
# Enable Web Management Service
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementService
Set-ItemProperty -Path HKLM:\SOFTWARE\Microsoft\WebManagement\Server -Name EnableRemoteManagement -Value 1
Start-Service WMSVC
Set-Service WMSVC -StartupType Automatic

# Install Web Deploy
# Download from: https://www.iis.net/downloads/microsoft/web-deploy
```

**GitHub Secrets needed:**
- `DEPLOY_USERNAME`: Web Deploy username
- `DEPLOY_PASSWORD`: Web Deploy password

---

### 🔒 **Option 3: SSH Deployment** (Most Secure)

**Requirements:**
- SSH access to remote Windows server
- PowerShell remoting enabled

**GitHub Secrets needed:**
- `REMOTE_USERNAME`: SSH/RDP username
- `REMOTE_PASSWORD`: SSH/RDP password
- OR `REMOTE_SSH_KEY`: SSH private key

---

## Quick Setup (FTP Method)

1. **Add FTP credentials to GitHub:**
   ```
   Repository → Settings → Secrets and variables → Actions → New repository secret
   
   Name: FTP_USERNAME
   Value: [your-ftp-username]
   
   Name: FTP_PASSWORD  
   Value: [your-ftp-password]
   ```

2. **Activate FTP deployment workflow:**

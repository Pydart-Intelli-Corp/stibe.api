# 🚀 Stibe.API - Complete Deployment Guide

> **Comprehensive deployment guide for Stibe.API with multiple deployment strategies**

**📅 Last Updated:** September 8, 2025  
**🔄 Version:** 2.0.0  
**🎯 Target:** Windows IIS Servers  
**🌐 Remote Server:** http://202.164.153.160:85

---

## 📋 Table of Contents

1. [🎯 Deployment Overview](#-deployment-overview)
2. [🏗️ Server Prerequisites](#️-server-prerequisites)
3. [🚀 Deployment Options](#-deployment-options)
4. [⚙️ FTP Deployment Setup](#️-ftp-deployment-setup)
5. [🤖 GitHub Actions Setup](#-github-actions-setup)
6. [🔧 Local Development Setup](#-local-development-setup)
7. [🛠️ Troubleshooting](#️-troubleshooting)
8. [🔒 Security Considerations](#-security-considerations)

---

## 🎯 Deployment Overview

This guide covers deploying the Stibe.API to IIS servers using multiple strategies:

- **🌐 Remote IIS Server**: `http://202.164.153.160:85`
- **🔗 Health Check Endpoint**: `http://202.164.153.160:85/api/test/health`
- **📁 FTP Access**: Port 92 with credentials (test / Access$404)
- **🏠 Local IIS Setup**: Self-hosted environment
- **🤖 CI/CD Pipeline**: GitHub Actions automation

### ✨ Supported Deployment Methods

| Method | Complexity | Security | Use Case |
|--------|------------|----------|----------|
| **FTP** | ⭐ Easy | ⭐⭐ Basic | Quick deployments |
| **Web Deploy** | ⭐⭐ Medium | ⭐⭐⭐ Good | Professional setups |
| **SSH/RDP** | ⭐⭐⭐ Advanced | ⭐⭐⭐⭐ High | Enterprise environments |
| **Self-Hosted Runner** | ⭐⭐ Medium | ⭐⭐⭐⭐ High | Local control |

---

## 🏗️ Server Prerequisites

### Windows Server Requirements
- Windows Server 2019/2022 or Windows 10/11 Pro
- Internet Information Services (IIS)
- .NET 8.0 Runtime & Hosting Bundle
- MySQL 8.0+ or SQL Server

### IIS Installation & Configuration
1. **Enable IIS Features:**
   - Open "Turn Windows features on or off"
   - Enable Internet Information Services (IIS)
   - Enable ASP.NET features under IIS
   - Enable IIS Management Console

2. **Install .NET 8.0 Hosting Bundle:**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Look for "Hosting Bundle" installer
   - Install and restart IIS

3. **Create Application Pool & Site:**
   - Open IIS Manager
   - Create new Application Pool named "StibeAPI"
   - Set .NET CLR version to "No Managed Code"
   - Create new Website on port 85
   - Assign the StibeAPI application pool

---

## 🚀 Deployment Options

### 🌐 **Option 1: FTP Deployment** (Recommended)

**✅ Current Server Configuration:**
- **FTP Server**: 202.164.153.160
- **FTP Port**: 92
- **Username**: test
- **Password**: Access$404
- **Target Directory**: /test/ (maps to C:\inetpub\wwwroot\test)
- **Health Check URL**: http://202.164.153.160:85/test/api/test/health

**📋 Requirements:**
- FTP client (FileZilla, WinSCP, or command line)
- Built application files from `dotnet publish`

**⚙️ Manual FTP Deployment Steps:**

1. **Build the Application:**
   - Open terminal in project directory
   - Run: `dotnet clean`
   - Run: `dotnet restore`
   - Run: `dotnet build --configuration Release`
   - Run: `dotnet publish --configuration Release --output ./publish`

2. **Connect via FTP:**
   - Use FTP client (FileZilla recommended)
   - Host: 202.164.153.160
   - Port: 92
   - Username: test
   - Password: Access$404

3. **Upload Files:**
   - Navigate to the publish folder on local machine
   - Upload all files and folders to /test/ directory on remote server
   - Ensure all DLL files, web.config, and appsettings.json are uploaded
   - Exclude development files (appsettings.Development.json, .pdb files)

4. **Verify Deployment:**
   - Wait 30 seconds for IIS to process new files
   - Visit: http://202.164.153.160:85/test/api/test/health
   - Should return successful health check response
   - Test additional endpoints as needed

**🤖 Automated Deployment (GitHub Actions):**
- Push code to master branch triggers automatic deployment
- Uses existing `.github/workflows/deploy-to-iis.yml`
- Includes automated testing and health checks
- Provides comprehensive deployment reports

### 🤖 **Option 2: GitHub Self-Hosted Runner**

**✅ Advantages:**
- Direct access to local resources
- No network restrictions
- Full control over deployment environment
- Faster deployments (no network transfer)

**📋 Requirements:**
- Windows machine with Administrator access
- Stable internet connection
- GitHub repository access

### 🌐 **Option 3: Web Deploy (MSDeploy)**

**✅ Advantages:**
- Professional deployment method
- Atomic deployments (all-or-nothing)
- Built-in rollback capabilities
- IIS integration

**📋 Requirements:**
- Web Deploy 3.6+ installed on server
- Web Management Service enabled
- Deploy user account configured

---

## ⚙️ FTP Deployment Setup

### Server Configuration Details

**🌐 Remote Server Information:**
- **Server Address**: 202.164.153.160
- **HTTP Port**: 85
- **FTP Port**: 92
- **Health Check**: http://202.164.153.160:85/api/test/health

**🔐 FTP Credentials:**
- **Username**: test
- **Password**: Access$404

### Manual Deployment Process

**1. Prepare Application Files:**
- Open command prompt in project root
- Clean previous builds: `dotnet clean`
- Restore packages: `dotnet restore`
- Build for release: `dotnet build --configuration Release`
- Publish application: `dotnet publish --configuration Release --output ./publish`

**2. FTP Client Setup:**
- Download and install FileZilla Client (recommended)
- Alternative: Use WinSCP or built-in Windows FTP

**3. Connect to FTP Server:**
- Open FileZilla
- Host: 202.164.153.160
- Port: 92
- Username: test
- Password: Access$404
- Click "Quickconnect"

**4. Upload Application Files:**
- Navigate to your local `./publish` folder
- Select all files and folders
- Drag and drop to remote server root directory
- Wait for upload to complete (may take several minutes)

**5. Verify Deployment:**
- Open browser
- Navigate to: http://202.164.153.160:85/api/test/health
- Confirm successful response
- Test other API endpoints as needed

---

## 🤖 GitHub Actions Setup

### Self-Hosted Runner Installation

### Self-Hosted Runner Installation

**Manual Setup Steps:**

1. **Download GitHub Runner:**
   - Go to your GitHub repository
   - Navigate to Settings → Actions → Runners
   - Click "New self-hosted runner"
   - Follow download instructions for Windows x64

2. **Configure Runner:**
   - Extract runner to C:\actions-runner
   - Run configuration command provided by GitHub
   - Install as Windows service for persistent operation

3. **Create Deployment Workflow:**
   - Create workflow file for self-hosted runner
   - Configure to build and deploy directly to local IIS
   - Set up application pool management during deployment

### GitHub Actions Configuration

**Current Workflow Configuration:**

The repository includes a complete GitHub Actions workflow at `.github/workflows/deploy-to-iis.yml` with the following setup:

**Environment Variables:**
- **DOTNET_VERSION**: 8.0.x
- **REMOTE_SERVER**: 202.164.153.160
- **REMOTE_PORT**: 85
- **FTP_PORT**: 92
- **API_HEALTH_ENDPOINT**: /api/test/health

**Workflow Features:**
- Triggered on push to master/main branch
- Manual trigger support (workflow_dispatch)
- Automated build and test execution
- NuGet package caching for faster builds
- FTP deployment to /test/ directory
- Automatic health check verification
- Comprehensive deployment reporting

**Required GitHub Secrets:**
1. **Set Repository Secrets:**
   - Go to GitHub repository → Settings → Secrets and variables → Actions
   - Add these secrets:
     - `FTP_USERNAME`: test
     - `FTP_PASSWORD`: Access$404

**Deployment Process:**
1. **Build Phase:**
   - Checkout code from repository
   - Setup .NET 8.0 SDK
   - Cache NuGet packages for performance
   - Restore dependencies and build in Release mode
   - Run tests (continues even if tests fail)
   - Publish application to ./publish directory

2. **Deploy Phase:**
   - Deploy via FTP to 202.164.153.160:92
   - Upload to /test/ directory on server
   - Exclude development files and debug symbols
   - Wait 30 seconds for IIS application restart

3. **Verification Phase:**
   - Test health endpoint: http://202.164.153.160:85/test/api/test/health
   - Test root endpoint accessibility
   - Provide comprehensive deployment report
   - Continue even if health check fails (files still deployed)

**Manual Trigger:**
- Go to GitHub repository → Actions tab
- Select "Deploy to Remote IIS Website" workflow
- Click "Run workflow" button
- Choose branch and click "Run workflow"

**Monitoring Deployment:**
- Check Actions tab for real-time deployment progress
- View detailed logs for each deployment step
- Automatic health check results in workflow output
- Deployment summary with server status and endpoints

---

## 🔧 Local Development Setup

### Development Environment Setup

**Local Development Steps:**

1. **Clone Repository:**
   - Clone: `git clone https://github.com/Pydart-Intelli-Corp/stibe.api.git`
   - Navigate to project directory

2. **Setup Dependencies:**
   - Restore NuGet packages: `dotnet restore`
   - Update database: `dotnet ef database update`
   - Configure connection strings in appsettings.Development.json

3. **Run Development Server:**
   - Start with: `dotnet run --environment Development`
   - API available at: https://localhost:5001 or http://localhost:5000

### Local IIS Testing Setup

**Create Local IIS Site:**

1. **Create Application Pool:**
   - Open IIS Manager
   - Add new Application Pool: "StibeAPI-Local"
   - Set .NET CLR version to "No Managed Code"

2. **Create Website:**
   - Add new Website: "StibeAPI-Local"
   - Port: 8080 (or available port)
   - Physical Path: C:\inetpub\wwwroot\StibeAPI-Local
   - Assign StibeAPI-Local application pool

3. **Set Permissions:**
   - Grant IIS_IUSRS full control to website directory
   - Grant IUSR read and execute permissions

---

## 🛠️ Troubleshooting

### Common Deployment Issues

### Common Deployment Issues

**1. Application Pool Stops After Deployment:**
- Check Windows Event Logs (Application section)
- Verify .NET 8.0 Runtime is installed on server
- Check file permissions on C:\inetpub\wwwroot\test directory
- Restart Application Pool "StibeAPI" in IIS Manager
- Ensure Application Pool is set to "No Managed Code"

**2. FTP Connection Issues:**
- Verify FTP server is running on port 92
- Test connection: telnet 202.164.153.160 92
- Check firewall settings allow port 92
- Confirm credentials: test / Access$404
- Ensure uploading to /test/ directory

**3. HTTP 500 Errors:**
- Check IIS error logs in C:\inetpub\logs\LogFiles
- Verify web.config is properly configured
- Ensure all required DLL files are deployed to /test/ directory
- Check database connection string in appsettings.json

**4. API Not Responding:**
- Test health endpoint: http://202.164.153.160:85/test/api/test/health
- Test root endpoint: http://202.164.153.160:85/test/
- Check if application pool is running
- Verify port 85 is not blocked by firewall
- Review application logs for errors

**5. GitHub Actions Deployment Failures:**
- Verify FTP_USERNAME and FTP_PASSWORD secrets are set correctly
- Check Actions tab for detailed error logs
- Confirm FTP server allows connections during deployment time
- Review workflow logs for specific failure points

### Diagnostic Steps

**Health Check Verification:**
- Primary endpoint: http://202.164.153.160:85/test/api/test/health
- Should return 200 OK with health status
- If failing, check IIS logs and application pool status
- GitHub Actions automatically tests this endpoint after deployment

**Service Status Checks:**
- Check IIS service is running (W3SVC and WAS)
- Verify Application Pool "StibeAPI" is started
- Confirm website is running on port 85
- Test FTP connectivity on port 92

**File Deployment Verification:**
- Ensure all files from publish folder are uploaded to /test/ directory
- Check web.config exists and is valid in /test/ folder
- Verify appsettings.json has correct configuration
- Confirm all DLL files are present in /test/ directory
- Check that appsettings.Development.json is NOT deployed (excluded by workflow)

---

## 🔒 Security Considerations

### Production Security Checklist

### Production Security Checklist

**✅ Server Security:**
- [ ] Windows Updates installed and current
- [ ] Firewall configured (allow ports 85, 92, 3389 only)
- [ ] Strong administrator passwords enforced
- [ ] Antivirus software active and updated
- [ ] Regular security audit schedule established

**✅ IIS Security:**
- [ ] Remove default IIS websites and applications
- [ ] Configure HTTPS with valid SSL certificate
- [ ] Set proper CORS policies in application
- [ ] Enable request filtering to block malicious requests
- [ ] Configure custom error pages (hide detailed errors)

**✅ Application Security:**
- [ ] Use secure connection strings in production
- [ ] JWT secret keys are strong (32+ characters)
- [ ] Enable HTTPS redirects in application
- [ ] Configure rate limiting for API endpoints
- [ ] Implement proper error handling and logging

**✅ FTP Security:**
- [ ] Change default FTP credentials regularly
- [ ] Use non-standard FTP port (currently 92)
- [ ] Limit FTP user permissions to minimum required
- [ ] Monitor FTP access logs regularly
- [ ] Consider FTPS (FTP over SSL) for sensitive data

**✅ Database Security:**
- [ ] Use dedicated database user with limited permissions
- [ ] Enable SSL connections to database
- [ ] Schedule regular database backups
- [ ] Monitor database access logs
- [ ] Keep database server updated

### Security Configuration Notes

**HTTPS Setup:**
- Configure SSL certificate in IIS
- Set up HTTP to HTTPS redirects
- Configure request filtering for file size limits
- Enable secure headers in application

**Production Configuration:**
- Use Warning level logging to reduce log volume
- Configure strong JWT secret keys (32+ characters minimum)
- Set AllowedHosts to specific domains only
- Configure appropriate token expiration times

---

## 📞 Support & Maintenance

### Regular Maintenance Tasks

**Weekly Maintenance:**
1. **Check System Health:**
   - Verify http://202.164.153.160:85/api/test/health responds correctly
   - Check disk space availability on server
   - Review Windows Event Logs for errors

2. **Log Management:**
   - Clear IIS logs older than 30 days (C:\inetpub\logs\LogFiles)
   - Review application logs for unusual activity
   - Check FTP access logs for unauthorized attempts

3. **Application Pool Management:**
   - Restart Application Pool weekly during maintenance window
   - Monitor memory usage of w3wp.exe processes
   - Check for any stopped or failing services

**Monthly Maintenance:**
- Install Windows security updates
- Review and rotate FTP credentials if needed
- Backup application configuration files
- Test backup and recovery procedures

### Support Information

**Quick Reference:**
- **Health Check**: http://202.164.153.160:85/test/api/test/health
- **Root API**: http://202.164.153.160:85/test/
- **FTP Access**: 202.164.153.160:92 (test / Access$404)
- **Target Directory**: /test/ (maps to C:\inetpub\wwwroot\test)
- **GitHub Actions**: Automatic deployment on master branch push
- **API Documentation**: Available via Swagger UI at /test/swagger endpoint
- **Repository**: https://github.com/Pydart-Intelli-Corp/stibe.api

**Workflow Information:**
- **Workflow File**: `.github/workflows/deploy-to-iis.yml`
- **Deployment Method**: FTP via SamKirkland/FTP-Deploy-Action@v4.3.5
- **Build Target**: .NET 8.0 Release configuration
- **Auto-Verification**: Health check and endpoint testing included
- **Manual Trigger**: Available via GitHub Actions interface

**For Technical Support:**
- Create issues in GitHub repository for bug reports
- Check Actions tab for deployment logs and errors
- Review comprehensive API documentation for detailed technical information
- Monitor deployment via GitHub Actions workflow results

---

*This deployment guide covers the complete setup process for the Stibe.API. For additional support or custom configurations, please refer to the repository documentation or create an issue on GitHub.*

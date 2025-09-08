# GitHub Secrets Setup Guide for Stibe API Deployment

## Overview
This guide will help you add the necessary FTP credentials to your GitHub repository secrets for automatic deployment.

## Required Secrets

You need to add the following secrets to your GitHub repository:

1. **FTP_USERNAME** - The FTP username for deployment
2. **FTP_PASSWORD** - The FTP password for deployment

## Step-by-Step Instructions

### 1. Access Your GitHub Repository
1. Go to your GitHub repository: `https://github.com/Pydart-Intelli-Corp/stibe.api`
2. Make sure you're logged in with appropriate permissions

### 2. Navigate to Repository Secrets
1. Click on **Settings** tab (top navigation)
2. In the left sidebar, click on **Secrets and variables**
3. Click on **Actions**

### 3. Add FTP_USERNAME Secret
1. Click **New repository secret**
2. Name: `FTP_USERNAME`
3. Value: `stibe-deploy` (or the username you created on the FTP server)
4. Click **Add secret**

### 4. Add FTP_PASSWORD Secret
1. Click **New repository secret**
2. Name: `FTP_PASSWORD`
3. Value: The password you set for the FTP user (e.g., `StibeAPI2025!`)
4. Click **Add secret**

### 5. Verify Secrets
After adding both secrets, you should see:
- FTP_USERNAME
- FTP_PASSWORD

Both should show "Updated X minutes ago" with green checkmarks.

## Security Best Practices

1. **Strong Passwords**: Use complex passwords for FTP users
2. **Limited Permissions**: FTP user should only have access to the deployment directory
3. **Regular Rotation**: Consider changing FTP passwords periodically
4. **Monitor Access**: Review FTP logs regularly for unauthorized access

## Testing the Setup

### 1. Test FTP Connection Locally
Before pushing to GitHub, test the FTP connection from your local machine:

```powershell
# Run this command in PowerShell
.\scripts\test-ftp-connection.ps1 -Username "stibe-deploy" -Password "YourPassword"
```

### 2. Test GitHub Actions Deployment
1. Make a small change to your code (e.g., update a comment)
2. Commit and push to the `master` branch:
   ```bash
   git add .
   git commit -m "Test deployment"
   git push origin master
   ```
3. Go to **Actions** tab in GitHub to monitor the deployment
4. Check the deployment logs for any errors

### 3. Verify Deployment
After successful deployment, verify your API:
- Health check: `http://202.164.153.160:85/test/api/test/health`
- Any other endpoints you want to test

## Troubleshooting

### Common Issues:

1. **FTP Connection Timeout**
   - Check firewall settings on remote server
   - Ensure FTP service is running
   - Verify port 21 is open

2. **Authentication Failed**
   - Double-check username and password in GitHub secrets
   - Verify user exists on remote server
   - Check user permissions

3. **Permission Denied**
   - Ensure FTP user has write permissions to `/test` directory
   - Check directory ownership and permissions

4. **Deployment Fails**
   - Check GitHub Actions logs for detailed error messages
   - Verify FTP server is accessible from GitHub's servers
   - Ensure all required secrets are set correctly

## Support

If you encounter issues:
1. Check the GitHub Actions logs first
2. Test FTP connection manually using the test script
3. Verify all server configurations are correct
4. Check Windows Event Logs on the remote server for FTP-related errors

## Next Steps After Setup

1. ✅ Set up FTP server on remote machine
2. ✅ Add GitHub secrets
3. ✅ Test FTP connection
4. ✅ Test deployment via GitHub Actions
5. ✅ Verify API functionality
6. 🔄 Set up monitoring and regular health checks

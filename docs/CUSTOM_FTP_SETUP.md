# GitHub Secrets Update for Custom FTP Configuration

## Your FTP Configuration
- **Server**: 202.164.153.160
- **API Port**: 85 (website access)
- **FTP Port**: 92 (custom FTP port)
- **Username**: test
- **Password**: Access$404
- **Deploy Path**: /test/

## GitHub Secrets Setup

### Step 1: Access GitHub Repository Secrets
1. Go to: https://github.com/Pydart-Intelli-Corp/stibe.api
2. Click **Settings** tab
3. Click **Secrets and variables** → **Actions**

### Step 2: Add/Update Secrets

#### FTP_USERNAME Secret:
- **Name**: `FTP_USERNAME`
- **Value**: `test`

#### FTP_PASSWORD Secret:
- **Name**: `FTP_PASSWORD`
- **Value**: `Access$404`

### Step 3: Verify Configuration

After adding secrets, your workflow will use:
- FTP Server: 202.164.153.160:92
- Website URL: http://202.164.153.160:85/test/
- Health Check: http://202.164.153.160:85/test/api/test/health

## Testing the Setup

### 1. Test FTP Connection Locally (Optional)
```powershell
# Test from your local machine
ftp 202.164.153.160 92
# Login: test / Access$404
```

### 2. Test Deployment
```bash
# Make a small change and push
git add .
git commit -m "Test custom FTP deployment on port 92"
git push origin master
```

### 3. Monitor Deployment
- Go to **Actions** tab in GitHub
- Watch the deployment progress
- Check for any errors in the logs

### 4. Verify API
- Health Check: http://202.164.153.160:85/test/api/test/health
- Root: http://202.164.153.160:85/test/

## Updated Workflow Summary

Your GitHub Actions workflow now:
✅ **Builds** the .NET 8.0 API
✅ **Publishes** with SSL certificate included  
✅ **Deploys** to FTP server on port 92
✅ **Uses** custom credentials (test/Access$404)
✅ **Tests** API health on port 85
✅ **Targets** /test/ directory

## Firewall Requirements

Make sure these ports are open on your server:
- **Port 85**: Website access (HTTP)
- **Port 92**: FTP deployment
- **Passive ports**: If using passive FTP mode

## Quick Troubleshooting

### If deployment fails:
1. **Check FTP credentials** in GitHub Secrets
2. **Verify FTP service** is running on port 92
3. **Check firewall** allows port 92
4. **Test FTP connection** manually
5. **Review GitHub Actions logs** for specific errors

### If API doesn't respond:
1. **Check IIS application pool** is running
2. **Verify website configuration** on port 85
3. **Check SSL certificate** is deployed correctly
4. **Review IIS logs** for errors

---

**Ready to test!** Push any code change to master branch to trigger the automated deployment.

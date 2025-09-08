# Step-by-Step Guide: Adding GitHub Secrets for FTP Deployment

## Your FTP Credentials (to be added as secrets)
- **FTP_USERNAME**: `test`
- **FTP_PASSWORD**: `Access$404`

## Method 1: Repository Secrets (Recommended)

### Step 1: Navigate to Your Repository
1. Go to: **https://github.com/Pydart-Intelli-Corp/stibe.api**
2. Make sure you're logged in with appropriate permissions

### Step 2: Access Repository Settings
1. Click the **"Settings"** tab (top navigation bar)
2. You should see it next to: Code, Issues, Pull requests, Actions, Projects, Wiki, Security, Insights, **Settings**

### Step 3: Navigate to Secrets and Variables
1. In the left sidebar, scroll down to **"Security"** section
2. Click **"Secrets and variables"**
3. Click **"Actions"** from the dropdown

### Step 4: Add FTP_USERNAME Secret
1. Click the **"New repository secret"** button (green button)
2. Fill in the form:
   - **Name**: `FTP_USERNAME` (exactly as shown)
   - **Secret**: `test` (your FTP username)
3. Click **"Add secret"**

### Step 5: Add FTP_PASSWORD Secret
1. Click **"New repository secret"** button again
2. Fill in the form:
   - **Name**: `FTP_PASSWORD` (exactly as shown)
   - **Secret**: `Access$404` (your FTP password)
3. Click **"Add secret"**

### Step 6: Verify Secrets
After adding both secrets, you should see:
```
Repository secrets
├── FTP_USERNAME (Updated X minutes ago)
└── FTP_PASSWORD (Updated X minutes ago)
```

## Method 2: Environment Secrets (Alternative)

If you want to use environment-specific secrets:

### Step 1: Create Environment
1. In repository Settings → **"Environments"**
2. Click **"New environment"**
3. Name: `production` or `deployment`
4. Click **"Configure environment"**

### Step 2: Add Environment Secrets
1. In the environment settings, scroll to **"Environment secrets"**
2. Click **"Add secret"**
3. Add the same secrets:
   - `FTP_USERNAME`: `test`
   - `FTP_PASSWORD`: `Access$404`

### Step 3: Update Workflow (if using environments)
Add environment to your workflow:
```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production  # Add this line
    steps:
    # ... rest of your workflow
```

## Visual Navigation Guide

```
GitHub Repository
└── Settings (top tab)
    └── Security (left sidebar)
        └── Secrets and variables
            └── Actions
                └── Repository secrets section
                    ├── New repository secret (button)
                    ├── FTP_USERNAME ✅
                    └── FTP_PASSWORD ✅
```

## Common Issues & Solutions

### Issue 1: "Settings" tab not visible
**Solution**: You need admin/write permissions on the repository

### Issue 2: Can't find "Secrets and variables"
**Solution**: Look in the left sidebar under the "Security" section

### Issue 3: Secret names are case-sensitive
**Solution**: Use exact names: `FTP_USERNAME` and `FTP_PASSWORD`

### Issue 4: Special characters in password
**Solution**: GitHub handles special characters like `$` automatically

## Testing Your Secrets

### Method 1: Check Workflow File
Your workflow should reference secrets like this:
```yaml
username: ${{ secrets.FTP_USERNAME }}
password: ${{ secrets.FTP_PASSWORD }}
```

### Method 2: Test Deployment
1. Make a small change to any file
2. Commit and push:
   ```bash
   git add .
   git commit -m "Test FTP secrets"
   git push origin master
   ```
3. Watch the GitHub Actions workflow run

### Method 3: Check Action Logs
1. Go to **"Actions"** tab in your repository
2. Click on the latest workflow run
3. Check for authentication errors

## Security Best Practices

✅ **Do use repository secrets for:**
- FTP credentials
- Database passwords
- API keys
- SSL certificates

❌ **Don't use secrets for:**
- Public configuration
- Non-sensitive data
- Server addresses (these can be in workflow files)

✅ **Repository Secrets vs Environment Secrets:**

| Repository Secrets | Environment Secrets |
|-------------------|-------------------|
| ✅ Simple setup | ✅ Environment-specific |
| ✅ Available to all workflows | ✅ Additional security controls |
| ✅ Good for single environment | ✅ Good for dev/staging/prod |
| ✅ **Recommended for your case** | ⚠️ More complex setup |

## After Adding Secrets

Once you've added the secrets, your workflow will:
1. ✅ Connect to `202.164.153.160:92` (FTP)
2. ✅ Login with `test` / `Access$404`
3. ✅ Deploy files to `/test/` directory
4. ✅ Test API at `http://202.164.153.160:85/test/api/test/health`

## Quick Verification Checklist

- [ ] Repository Settings accessible
- [ ] "Secrets and variables" → "Actions" found
- [ ] FTP_USERNAME secret added with value: `test`
- [ ] FTP_PASSWORD secret added with value: `Access$404`
- [ ] Both secrets show "Updated X minutes ago"
- [ ] Workflow file references `${{ secrets.FTP_USERNAME }}`
- [ ] Workflow file references `${{ secrets.FTP_PASSWORD }}`

Ready to test your automated deployment! 🚀

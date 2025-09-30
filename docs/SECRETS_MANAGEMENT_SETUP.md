# Secrets Management Setup Guide

## Overview
This guide explains how to set up the new secrets management system for the Stibe API project. We've moved all sensitive information from `appsettings.json` to separate files to enhance security.

## Files Structure

### Configuration Files
- `appsettings.json` - Contains placeholders (#{SecretName}#) for secrets and non-sensitive configuration
- `appsettings.Secrets.json` - Contains actual secret values for local development (NEVER committed to Git)
- `appsettings.Production.json` - Contains placeholders for production deployment
- `appsettings.Development.json` - Contains placeholders for development environment

### Git Ignore
- `appsettings.Secrets.json` is added to `.gitignore` to prevent accidental commits

## Local Development Setup

### Step 1: Use the Secrets File
For local development, you should use the `appsettings.Secrets.json` file that contains the actual secret values.

Update your `Program.cs` or startup configuration to load this file in development:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);
}
```

### Step 2: Environment Variables (Alternative)
Alternatively, you can set environment variables for local development:

```bash
# Windows PowerShell
$env:ConnectionStrings__DefaultConnection = "Server=..."
$env:JwtSettings__SecretKey = "your-secret-key..."
# ... other secrets

# Or create a .env file and use a library like DotNetEnv
```

## GitHub Secrets Setup

You need to add the following secrets to your GitHub repository:

### Navigation: Repository → Settings → Secrets and variables → Actions → New repository secret

Add these secrets:

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `CONNECTION_STRING` | Database connection string | `Server=psrazuredb.mysql.database.azure.com;Port=3306;UserID=psrcloud;Password=Access@LRC2404;Database=Stibe_db;SslMode=Required;SslCa=config/certificates/DigiCertGlobalRootCA.crt.pem` |
| `JWT_SECRET_KEY` | JWT signing key | `production-super-secret-key-that-should-be-64-characters-long-and-very-secure-for-production-use-only` |
| `GOOGLE_CLIENT_ID` | Google OAuth Client ID | `325397741771-51krs31aab215vbddilqhdna9bm2ndbr.apps.googleusercontent.com` |
| `GOOGLE_CLIENT_SECRET` | Google OAuth Client Secret | `GOCSPX-your_actual_web_client_secret_here` |
| `GOOGLE_ANDROID_CLIENT_ID` | Google Android Client ID | `325397741771-51krs31aab215vbddilqhdna9bm2ndbr.apps.googleusercontent.com` |
| `GOOGLE_WEB_CLIENT_ID` | Google Web Client ID | `325397741771-6eqm9n2ptldamgnl1spo24ljf7psrgr5.apps.googleusercontent.com` |
| `AZURE_STORAGE_CONNECTION_STRING` | Azure Storage connection | `DefaultEndpointsProtocol=https;AccountName=stibestorage;AccountKey=...` |
| `AZURE_STORAGE_SAS_TOKEN` | Azure Storage SAS token | `sp=racwdli&st=2025-09-30T17:06:01Z&se=...` |
| `SMTP_USERNAME` | SMTP username | `info.pydart@gmail.com` |
| `SMTP_PASSWORD` | SMTP password | `fkde vkem iuau nzjl` |
| `SMTP_SENDER_EMAIL` | SMTP sender email | `info.pydart@gmail.com` |
| `RAZORPAY_KEY_ID` | Razorpay Key ID | `rzp_live_RJTwtHf8nT8bat` |
| `RAZORPAY_KEY_SECRET` | Razorpay Key Secret | `TXKbRipKDJQ9hI1bYzyf715Q` |
| `RAZORPAY_WEBHOOK_SECRET` | Razorpay Webhook Secret | `your_live_webhook_secret` |

## Azure Deployment

The GitHub Actions workflow automatically replaces placeholders with actual secret values during deployment. The secrets are injected during the build process and never stored in the repository.

### Workflow Process:
1. Code is checked out
2. .NET SDK is set up
3. **Secrets are replaced** in both `appsettings.json` and `appsettings.Production.json`
4. Application is built and tested
5. Application is published
6. Deployed to Azure

## Security Benefits

1. **No secrets in repository**: All sensitive data is excluded from version control
2. **Environment-specific**: Different secrets can be used for development, staging, and production
3. **GitHub managed**: Secrets are encrypted and managed by GitHub's secure infrastructure
4. **Audit trail**: Changes to secrets are logged in GitHub
5. **Team access**: Team members can be granted access to secrets without knowing the actual values

## Best Practices

1. **Rotate secrets regularly**: Update secrets periodically, especially for production
2. **Use strong passwords**: Ensure all secrets use strong, unique values
3. **Principle of least privilege**: Only grant access to secrets to those who need them
4. **Monitor usage**: Keep track of when and how secrets are used
5. **Backup recovery**: Ensure you have secure backups of critical secrets

## Troubleshooting

### Local Development Issues
- Ensure `appsettings.Secrets.json` exists and contains all required secrets
- Check that the file is properly formatted JSON
- Verify file permissions allow reading

### GitHub Actions Issues
- Verify all required secrets are added to the GitHub repository
- Check that secret names match exactly (case-sensitive)
- Review GitHub Actions logs for any replacement errors

### Azure Deployment Issues
- Ensure Azure Web App is configured correctly
- Check that the publish profile secret is valid
- Verify network connectivity and permissions

## Migration Checklist

- [x] Create `appsettings.Secrets.json` with actual secret values
- [x] Update `appsettings.json` with placeholders
- [x] Create `appsettings.Production.json` for deployment
- [x] Add secrets file to `.gitignore`
- [x] Update GitHub Actions workflow to replace secrets
- [ ] Add all secrets to GitHub repository settings
- [ ] Update local development configuration to use secrets file
- [ ] Test local development environment
- [ ] Test GitHub Actions deployment
- [ ] Verify Azure deployment works correctly

## Next Steps

1. Add all the GitHub secrets listed above to your repository
2. Update your local development configuration to use the secrets file
3. Test the deployment pipeline
4. Monitor the first few deployments to ensure everything works correctly

Remember: Never commit the `appsettings.Secrets.json` file to version control!
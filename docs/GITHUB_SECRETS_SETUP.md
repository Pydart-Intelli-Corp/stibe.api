# GitHub Secrets Setup for Azure Deployment

This document explains how to configure GitHub repository secrets for automated deployment to Azure.

## Required GitHub Repository Secrets

You need to add the following secrets to your GitHub repository at `Settings > Secrets and variables > Actions`:

### Database Configuration

- **`DB_CONNECTION_STRING`**: Your MySQL database connection string from `config/secrets/appsettings.Secrets.json`

### JWT Configuration

- **`JWT_SECRET_KEY`**: Your JWT secret key from `config/secrets/appsettings.Secrets.json` (minimum 64 characters)

### Google OAuth Configuration

- **`GOOGLE_CLIENT_ID`**: Your Google OAuth client ID from `config/secrets/appsettings.Secrets.json`
- **`GOOGLE_CLIENT_SECRET`**: Your Google OAuth client secret from `config/secrets/appsettings.Secrets.json`
- **`GOOGLE_ANDROID_CLIENT_ID`**: Your Google Android client ID from `config/secrets/appsettings.Secrets.json`
- **`GOOGLE_WEB_CLIENT_ID`**: Your Google Web client ID from `config/secrets/appsettings.Secrets.json`

### Azure Storage Configuration

- **`AZURE_STORAGE_CONNECTION_STRING`**: Your Azure Storage connection string from `config/secrets/appsettings.Secrets.json`
- **`AZURE_STORAGE_SAS_TOKEN`**: Your Azure Storage SAS token from `config/secrets/appsettings.Secrets.json`

### SMTP Configuration

- **`SMTP_USERNAME`**: Your SMTP username from `config/secrets/appsettings.Secrets.json`
- **`SMTP_PASSWORD`**: Your SMTP password from `config/secrets/appsettings.Secrets.json`

### Razorpay Configuration

- **`RAZORPAY_KEY_ID`**: Your Razorpay key ID from `config/secrets/appsettings.Secrets.json`
- **`RAZORPAY_KEY_SECRET`**: Your Razorpay key secret from `config/secrets/appsettings.Secrets.json`
- **`RAZORPAY_WEBHOOK_SECRET`**: Your Razorpay webhook secret from `config/secrets/appsettings.Secrets.json`

### Azure Deployment

- **`AZURE_WEBAPP_PUBLISH_PROFILE`**: Download this from your Azure App Service > Get publish profile

## How to Add Secrets

1. Go to your GitHub repository
2. Click on `Settings` tab
3. In the left sidebar, click on `Secrets and variables` > `Actions`
4. Click `New repository secret`
5. Enter the secret name and value from your `config/secrets/appsettings.Secrets.json` file
6. Click `Add secret`

## Automated Setup

Use the provided PowerShell script to automatically set up all secrets:

```powershell
.\Scripts\setup-github-secrets.ps1 -GitHubOwner "your-username" -GitHubRepo "your-repo" -GitHubToken "your-token"
```

## Security Notes

- Never commit secrets to your repository
- Keep your `appsettings.Secrets.json` file in `.gitignore`
- Regularly rotate your secrets for security
- Use environment-specific secrets for different deployment environments
- All secret values should be copied exactly from your `config/secrets/appsettings.Secrets.json` file

## Deployment Process

The GitHub Actions workflow will:

1. Build the application
2. Create `appsettings.json` with production secrets from GitHub repository secrets
3. Publish the application
4. Deploy to Azure Web App

This ensures that sensitive configuration values are never stored in your repository but are securely injected during deployment.
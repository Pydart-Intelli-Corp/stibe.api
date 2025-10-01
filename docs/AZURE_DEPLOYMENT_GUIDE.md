# Azure Deployment Setup Guide

This guide will help you set up the Stibe API for production deployment on Azure App Service using GitHub Actions.

## Prerequisites

1. **Azure Subscription** with an App Service plan
2. **GitHub repository** with the stibe.api code
3. **Azure resources**:
   - Azure App Service (Web App)
   - Azure Database for MySQL
   - Azure Storage Account
   - Azure Key Vault (recommended for secrets)

## Step 1: Azure App Service Setup

### Create Azure Web App
```bash
# Create resource group
az group create --name stibe-rg --location "East US"

# Create App Service plan
az appservice plan create --name stibe-plan --resource-group stibe-rg --sku B1 --is-linux

# Create Web App
az webapp create --resource-group stibe-rg --plan stibe-plan --name stibe --runtime "DOTNETCORE|8.0"
```

### Download Publish Profile
1. Go to Azure Portal → App Services → stibe
2. Click "Get publish profile" to download the `.PublishSettings` file
3. Copy the entire contents of this file

## Step 2: GitHub Secrets Configuration

Add the following secrets to your GitHub repository (Settings → Secrets and variables → Actions):

### Required Secrets

#### Azure Deployment
- `AZURE_WEBAPP_PUBLISH_PROFILE`: Contents of the .PublishSettings file

#### Database
- `DB_CONNECTION_STRING`: MySQL connection string
  ```
  Server=your-mysql-server.mysql.database.azure.com;Port=3306;UserID=admin;Password=your-password;Database=Stibe_prod_db;SslMode=Required;
  ```

#### JWT Authentication
- `JWT_SECRET_KEY`: Strong secret key (64+ characters)
  ```
  production-super-secret-key-that-should-be-64-characters-long-and-very-secure-for-production-use-only
  ```

#### Google OAuth
- `GOOGLE_CLIENT_ID`: Google OAuth client ID
- `GOOGLE_CLIENT_SECRET`: Google OAuth client secret
- `GOOGLE_ANDROID_CLIENT_ID`: Android app client ID
- `GOOGLE_WEB_CLIENT_ID`: Web app client ID

#### Azure Storage
- `AZURE_STORAGE_CONNECTION_STRING`: Azure Storage connection string
- `AZURE_STORAGE_BASE_URL`: Base URL for Azure Storage
- `AZURE_STORAGE_SAS_TOKEN`: SAS token for container access

#### Email (SMTP)
- `SMTP_USERNAME`: Gmail username (info.pydart@gmail.com)
- `SMTP_PASSWORD`: Gmail app password
- `SMTP_SENDER_EMAIL`: Sender email address

#### Payment (Razorpay)
- `RAZORPAY_KEY_ID`: Live Razorpay key ID
- `RAZORPAY_KEY_SECRET`: Live Razorpay key secret
- `RAZORPAY_WEBHOOK_SECRET`: Webhook secret

#### Optional
- `RAZORPAY_COMPANY_LOGO`: URL to company logo
- `ALERT_EMAIL_ADDRESS`: Email for monitoring alerts

## Step 3: Azure Database Setup

### Create MySQL Database
```bash
# Create Azure Database for MySQL
az mysql flexible-server create \
  --resource-group stibe-rg \
  --name stibe-mysql \
  --admin-user stibeadmin \
  --admin-password "YourStrongPassword123!" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --public-access 0.0.0.0 \
  --version 8.0.21

# Create database
az mysql flexible-server db create \
  --resource-group stibe-rg \
  --server-name stibe-mysql \
  --database-name Stibe_prod_db
```

### Configure Firewall
```bash
# Allow Azure services
az mysql flexible-server firewall-rule create \
  --resource-group stibe-rg \
  --name stibe-mysql \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

## Step 4: Azure Storage Setup

### Create Storage Account
```bash
# Create storage account
az storage account create \
  --name stibestorage \
  --resource-group stibe-rg \
  --location "East US" \
  --sku Standard_LRS

# Create containers
az storage container create --name stibe-datas --account-name stibestorage
az storage container create --name profile-images --account-name stibestorage
az storage container create --name shop-images --account-name stibestorage
az storage container create --name service-images --account-name stibestorage
az storage container create --name product-images --account-name stibestorage
az storage container create --name receipts --account-name stibestorage
az storage container create --name apk-files --account-name stibestorage
```

## Step 5: Configuration Approach

The application uses a **unified configuration approach**:

### Single Configuration File Strategy
- **Base**: `appsettings.json` contains structure with placeholder variables (committed to repo)
- **Secrets**: `config/secrets/appsettings.Secrets.json` contains all real values (gitignored locally, generated during deployment)

### Environment Handling
All environments (Development, Staging, Production) use the same configuration structure. The secrets file provides the actual values, while the base configuration provides the structure and placeholders.

### Deployment Process
1. **Local Development**: Uses the gitignored secrets file in `config/secrets/`
2. **Azure Deployment**: GitHub Actions creates the secrets file from repository secrets during deployment

## Step 6: Deployment Process

### Automatic Deployment
1. Push code to `main` or `master` branch
2. GitHub Actions will:
   - Build the application
   - Run tests
   - Deploy to Azure App Service
   - Configure app settings with secrets

### Manual Deployment
```bash
# Build and publish locally
dotnet publish -c Release -o ./publish

# Deploy using Azure CLI
az webapp deployment source config-zip \
  --resource-group stibe-rg \
  --name stibe \
  --src ./publish.zip
```

## Step 7: Database Migration

### Run Migrations
```bash
# Set connection string
export ConnectionStrings__DefaultConnection="Server=stibe-mysql.mysql.database.azure.com;..."

# Update database
dotnet ef database update
```

## Step 8: Monitoring and Logging

### Application Insights (Optional)
```bash
# Create Application Insights
az monitor app-insights component create \
  --app stibe-insights \
  --location "East US" \
  --resource-group stibe-rg \
  --application-type web
```

### Log Streaming
```bash
# View live logs
az webapp log tail --resource-group stibe-rg --name stibe
```

## Step 9: Custom Domain and SSL (Optional)

### Add Custom Domain
```bash
# Add custom domain
az webapp config hostname add \
  --resource-group stibe-rg \
  --webapp-name stibe \
  --hostname api.yourdomain.com

# Enable SSL
az webapp config ssl bind \
  --resource-group stibe-rg \
  --name stibe \
  --certificate-thumbprint <thumbprint> \
  --ssl-type SNI
```

## Step 10: Health Checks

After deployment, verify the application:

1. **Health Endpoint**: `https://stibe.azurewebsites.net/health`
2. **API Documentation**: `https://stibe.azurewebsites.net/swagger`
3. **Version Info**: `https://stibe.azurewebsites.net/api/version`

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Verify connection string format
   - Check firewall rules
   - Ensure SSL is enabled

2. **Storage Access Errors**
   - Verify Azure Storage connection string
   - Check container permissions
   - Validate SAS token

3. **Authentication Issues**
   - Check JWT secret key
   - Verify Google OAuth configuration
   - Ensure environment variables are set

### Log Analysis
```bash
# Download logs
az webapp log download --resource-group stibe-rg --name stibe

# View live logs
az webapp log tail --resource-group stibe-rg --name stibe
```

## Security Best Practices

1. **Use Azure Key Vault** for sensitive configuration
2. **Enable HTTPS** redirect
3. **Configure CORS** properly
4. **Use managed identities** where possible
5. **Enable App Service authentication** if needed
6. **Regular security updates** and dependency scanning

## Cost Optimization

1. **Use Basic tier** for development/staging
2. **Scale up/down** based on usage
3. **Use reserved instances** for production
4. **Monitor usage** with Azure Cost Management

## Support

For issues with deployment:
1. Check Azure Activity Log
2. Review Application Logs
3. Monitor Application Insights
4. Contact support team

---

**Last Updated**: October 1, 2025
**Version**: 1.0.0
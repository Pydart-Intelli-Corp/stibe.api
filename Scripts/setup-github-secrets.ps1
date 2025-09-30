# GitHub Secrets Setup Script for Stibe API
# This script helps you set up GitHub repository secrets for automated Azure deployment

param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubOwner,
    
    [Parameter(Mandatory=$true)]
    [string]$GitHubRepo,
    
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken
)

# Check if GitHub CLI is installed
if (-not (Get-Command "gh" -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed. Please install it from https://cli.github.com/"
    exit 1
}

# Authenticate with GitHub CLI
Write-Host "Authenticating with GitHub..." -ForegroundColor Green
echo $GitHubToken | gh auth login --with-token

# Set repository context
$repo = "$GitHubOwner/$GitHubRepo"
Write-Host "Setting up secrets for repository: $repo" -ForegroundColor Green

# Read secrets from appsettings.Secrets.json
$secretsPath = "config\secrets\appsettings.Secrets.json"
if (-not (Test-Path $secretsPath)) {
    Write-Error "Secrets file not found at: $secretsPath"
    exit 1
}

$secrets = Get-Content $secretsPath | ConvertFrom-Json

# Function to set GitHub secret
function Set-GitHubSecret {
    param($name, $value)
    
    if ([string]::IsNullOrEmpty($value)) {
        Write-Warning "Skipping empty secret: $name"
        return
    }
    
    try {
        gh secret set $name --body $value --repo $repo
        Write-Host "✅ Set secret: $name" -ForegroundColor Green
    }
    catch {
        Write-Error "❌ Failed to set secret: $name - $($_.Exception.Message)"
    }
}

Write-Host "`nSetting up GitHub repository secrets..." -ForegroundColor Yellow

# Database Configuration
Set-GitHubSecret "DB_CONNECTION_STRING" $secrets.ConnectionStrings.DefaultConnection

# JWT Configuration
Set-GitHubSecret "JWT_SECRET_KEY" $secrets.JwtSettings.SecretKey

# Google OAuth Configuration
Set-GitHubSecret "GOOGLE_CLIENT_ID" $secrets.GoogleOAuth.ClientId
Set-GitHubSecret "GOOGLE_CLIENT_SECRET" $secrets.GoogleOAuth.ClientSecret
Set-GitHubSecret "GOOGLE_ANDROID_CLIENT_ID" $secrets.GoogleOAuth.AndroidClientId
Set-GitHubSecret "GOOGLE_WEB_CLIENT_ID" $secrets.GoogleOAuth.WebClientId

# Azure Storage Configuration
Set-GitHubSecret "AZURE_STORAGE_CONNECTION_STRING" $secrets.FileStorage.Azure.ConnectionString
Set-GitHubSecret "AZURE_STORAGE_SAS_TOKEN" $secrets.FileStorage.Azure.ContainerSasToken

# SMTP Configuration
Set-GitHubSecret "SMTP_USERNAME" $secrets.SmtpSettings.Username
Set-GitHubSecret "SMTP_PASSWORD" $secrets.SmtpSettings.Password

# Razorpay Configuration
Set-GitHubSecret "RAZORPAY_KEY_ID" $secrets.Razorpay.KeyId
Set-GitHubSecret "RAZORPAY_KEY_SECRET" $secrets.Razorpay.KeySecret
Set-GitHubSecret "RAZORPAY_WEBHOOK_SECRET" $secrets.Razorpay.WebhookSecret

Write-Host "`n✅ GitHub secrets setup completed!" -ForegroundColor Green
Write-Host "Don't forget to add the AZURE_WEBAPP_PUBLISH_PROFILE secret manually from your Azure App Service." -ForegroundColor Yellow

# Instructions for Azure publish profile
Write-Host "`nTo get your Azure Web App publish profile:" -ForegroundColor Cyan
Write-Host "1. Go to your Azure App Service in the Azure portal"
Write-Host "2. Click 'Get publish profile' in the overview page"
Write-Host "3. Copy the entire XML content"
Write-Host "4. Add it as AZURE_WEBAPP_PUBLISH_PROFILE secret in GitHub"

Write-Host "`nUsage example:" -ForegroundColor Cyan
Write-Host ".\setup-github-secrets.ps1 -GitHubOwner 'your-username' -GitHubRepo 'your-repo' -GitHubToken 'your-token'"
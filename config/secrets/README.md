# Secrets Configuration

This directory contains sensitive configuration files that should not be committed to version control.

## Files

### `appsettings.Secrets.json`
Contains sensitive configuration values that override the placeholder values in `appsettings.json`.

**Structure:**
```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Your database connection string"
    },
    "JwtSettings": {
        "SecretKey": "Your JWT secret key (64+ characters)"
    },
    "GoogleOAuth": {
        "ClientId": "Your Google OAuth client ID",
        "ClientSecret": "Your Google OAuth client secret",
        "AndroidClientId": "Your Android client ID",
        "WebClientId": "Your Web client ID"
    },
    "FileStorage": {
        "Azure": {
            "ConnectionString": "Your Azure storage connection string",
            "ContainerSasToken": "Your Azure container SAS token"
        }
    },
    "SmtpSettings": {
        "Username": "Your SMTP username",
        "Password": "Your SMTP password"
    },
    "Razorpay": {
        "KeyId": "Your Razorpay key ID",
        "KeySecret": "Your Razorpay key secret",
        "WebhookSecret": "Your Razorpay webhook secret"
    }
}
```

## Security Notes

1. **Never commit secrets files to version control**
2. Add `config/secrets/*.json` to your `.gitignore` file
3. Use environment variables for production deployments
4. Rotate secrets regularly
5. Use different secrets for development, staging, and production environments

## Environment Variables Alternative

For production deployments, consider using environment variables instead:

- `ConnectionStrings__DefaultConnection`
- `JwtSettings__SecretKey`
- `GoogleOAuth__ClientId`
- `GoogleOAuth__ClientSecret`
- `FileStorage__Azure__ConnectionString`
- `SmtpSettings__Username`
- `SmtpSettings__Password`
- `Razorpay__KeyId`
- `Razorpay__KeySecret`

## Setup Instructions

1. Copy the example secrets file and fill in your actual values
2. Ensure the file is readable by the application process
3. Verify that secrets are not being logged or exposed in error messages
4. Test configuration loading on application startup
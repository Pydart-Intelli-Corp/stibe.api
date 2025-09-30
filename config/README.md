# Configuration Files

This directory contains all configuration-related files for the Stibe API.

## Structure

### 📁 certificates/
SSL certificates and security-related files.
- `DigiCertGlobalRootCA.crt.pem` - Root certificate for SSL

### 📁 environments/
Environment-specific configuration files.
- `appsettings.Development.json` - Development environment settings
- `appsettings.Production.json` - Production environment settings
- `appsettings.Staging.json` - Staging environment settings

### 📁 secrets/
**⚠️ NEVER COMMIT THESE FILES TO GIT ⚠️**
Sensitive configuration files containing:
- Database connection strings with passwords
- API keys and secrets
- Google credentials JSON files
- Private certificates

### 📄 appsettings.json
Main application configuration file containing:
- Default settings for all environments
- Non-sensitive configuration values
- Feature flags
- Service configuration

## Security Notes

1. **Always** store sensitive data in `secrets/` folder
2. **Never** commit files from `secrets/` to version control
3. Use environment variables in production
4. Regularly rotate API keys and certificates
5. Use Azure Key Vault or similar for production secrets

## Environment Variables

In production, override sensitive settings using environment variables:

```bash
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=ProdDB;..."
export JwtSettings__SecretKey="your-production-jwt-key"
export GoogleOAuth__ClientSecret="your-google-client-secret"
```
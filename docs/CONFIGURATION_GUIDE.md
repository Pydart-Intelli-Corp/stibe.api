# Configuration Management - Unified Approach

## Overview

The Stibe API uses a **unified configuration approach** where all environments use the same configuration structure with a single secrets file containing all sensitive values.

## File Structure

```
├── appsettings.json                           # Base configuration with placeholders (committed)
└── config/
    └── secrets/
        └── appsettings.Secrets.json          # All real values (gitignored)
```

## How It Works

### 1. Base Configuration (`appsettings.json`)
Contains the configuration structure with placeholder variables:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${DB_CONNECTION_STRING}"
  },
  "JwtSettings": {
    "SecretKey": "${JWT_SECRET_KEY}"
  }
}
```

### 2. Secrets File (`config/secrets/appsettings.Secrets.json`)
Contains all the actual values that replace the placeholders:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=actual-server;..."
  },
  "JwtSettings": {
    "SecretKey": "actual-secret-key"
  }
}
```

### 3. Runtime Resolution
At startup, the application:
1. Loads `appsettings.json` (base structure)
2. Loads `config/secrets/appsettings.Secrets.json` (actual values)
3. The secrets file overrides the placeholder values

## Benefits

✅ **Single Source of Truth**: One secrets file for all environments
✅ **No Configuration Drift**: Same structure everywhere
✅ **Simplified Deployment**: No environment-specific files to maintain
✅ **Enhanced Security**: All secrets in one gitignored location
✅ **Easy Maintenance**: Update values in one place

## Environment Handling

### Local Development
- Secrets file exists locally in `config/secrets/`
- File is gitignored for security

### Azure Deployment
- GitHub Actions creates the secrets file from repository secrets
- File is deployed with the application package

## Required Secrets

The secrets file must contain:
- Database connection strings
- JWT secret keys
- OAuth client credentials
- Azure Storage connection strings
- SMTP credentials
- Payment gateway keys

## Security

- ⚠️ **Never commit the secrets file**
- ✅ The `config/secrets/` directory is gitignored
- ✅ GitHub repository secrets are used for deployment
- ✅ All sensitive values are centralized

---

This approach ensures consistent, secure, and maintainable configuration management across all environments.
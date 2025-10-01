# Development Environment Setup

This guide explains how to set up the Stibe API for local development.

## Configuration Structure

The project uses a **unified configuration approach** with a single secrets file:

1. **Base Configuration**: `appsettings.json` - Contains default structure with placeholders
2. **Secrets Override**: `config/secrets/appsettings.Secrets.json` - Contains all actual values for all environments (gitignored)

## Development Setup

### 1. Clone and Setup

```bash
git clone <repository-url>
cd stibe.api
```

### 2. Configure Secrets File

**Critical**: The application requires `config/secrets/appsettings.Secrets.json` to run in ANY environment.

This file contains all the real connection strings, API keys, and sensitive configuration values.

Location: `config/secrets/appsettings.Secrets.json`

### 3. Database Setup

- Ensure your database connection string is correct in the secrets file
- Run migrations:

```bash
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

The application will:
- Load the base configuration from `appsettings.json`
- Override all placeholder values with real values from the secrets file
- Use the same configuration structure across all environments

## Configuration Philosophy

**Single Source of Truth**: Instead of maintaining separate configuration files for different environments, we use:
- One `appsettings.json` with placeholder variables (safe to commit)
- One `config/secrets/appsettings.Secrets.json` with real values (gitignored)

This approach:
- ✅ Eliminates configuration drift between environments
- ✅ Simplifies deployment and maintenance
- ✅ Ensures consistent behavior across all environments
- ✅ Keeps all secrets in one secure, gitignored location

## Security Notes

- The `config/secrets/` directory is gitignored
- All sensitive values are stored in the single secrets file
- The secrets file is required for the application to start
- For production deployment, GitHub Actions creates the secrets file from repository secrets

## Testing

Run tests with:

```bash
dotnet test
```

## API Documentation

Once running, access Swagger UI at:
- Local: `https://localhost:5001/swagger`
- Health check: `https://localhost:5001/health`
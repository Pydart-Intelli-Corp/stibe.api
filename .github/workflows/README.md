# GitHub Actions Deployment Workflow

## Overview

This GitHub Actions workflow (`deploy-to-iis.yml`) automatically builds, publishes, and deploys the Stibe API to a remote IIS server using FTP.

## Workflow Features

- ✅ **Automated Build**: Builds the .NET 8.0 application in Release mode
- ✅ **Testing**: Runs unit tests (if available)
- ✅ **Database Migrations**: Optionally runs Entity Framework migrations
- ✅ **Publishing**: Creates a deployment-ready package
- ✅ **FTP Deployment**: Uploads files to the remote IIS server
- ✅ **Health Checks**: Verifies the deployment was successful
- ✅ **Artifact Upload**: Stores deployment package for troubleshooting

## Required GitHub Secrets

Configure the following secrets in your GitHub repository settings:

### Required Secrets

1. **`FTP_USERNAME`** - Username for FTP access to the IIS server
2. **`FTP_PASSWORD`** - Password for FTP access to the IIS server

### Optional Secrets

3. **`DATABASE_CONNECTION_STRING`** - Database connection string for migrations
   - If not provided, migrations will be skipped
   - Format: `Server=your-server;Database=your-db;User Id=user;Password=pass;`

## Server Configuration

The workflow is configured for the following server setup:

- **Server IP**: `202.164.153.160`
- **HTTP Port**: `85`
- **FTP Port**: `92`
- **Deployment Path**: `/test/` (maps to `C:\inetpub\wwwroot\test\`)

### IIS Requirements

Ensure your IIS server has:

1. **.NET 8.0 Runtime** installed (ASP.NET Core Runtime)
2. **Application Pool** configured with:
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: **Integrated**
3. **Website binding** on port `85`
4. **FTP Server** running on port `92`

## Deployment Process

### Automatic Triggers

The workflow runs automatically when:
- Code is pushed to `master` or `main` branch
- Manual trigger via GitHub Actions UI

### Manual Deployment

1. Go to GitHub Actions tab in your repository
2. Select "Deploy to Remote IIS Website"
3. Click "Run workflow"
4. Choose the branch and click "Run workflow"

## Monitoring Deployment

### During Deployment

The workflow provides detailed logging for each step:
- Build progress
- Test results
- Migration status
- FTP upload progress
- Health check results

### After Deployment

The workflow tests these endpoints:
- **Health Check**: `http://202.164.153.160:85/api/test/health`
- **Root API**: `http://202.164.153.160:85/test/`

### Deployment Artifacts

Each deployment creates an artifact containing:
- All published files
- Configuration files
- Dependencies
- Available for 7 days for troubleshooting

## Troubleshooting

### Common Issues

#### 1. FTP Connection Failed
- Verify FTP credentials in GitHub secrets
- Check if FTP port 92 is accessible
- Ensure FTP server is running on target server

#### 2. Health Check Failed
- Verify IIS Application Pool is running
- Check .NET 8.0 Runtime is installed
- Review IIS application logs
- Verify web.config is correct

#### 3. Build Failed
- Check for compilation errors in logs
- Verify all NuGet packages are available
- Check for missing dependencies

#### 4. Migration Failed
- Verify database connection string
- Check database server accessibility
- Review Entity Framework configuration

### Log Locations

On the IIS server, check these locations for logs:
- **Application Logs**: `C:\inetpub\wwwroot\test\logs\`
- **IIS Logs**: `C:\inetpub\logs\LogFiles\`
- **Event Viewer**: Windows Logs > Application

### Manual Verification

If automated health checks fail, manually verify:

1. **File Deployment**:
   ```
   Check if files exist in: C:\inetpub\wwwroot\test\
   Key files: stibe.api.dll, web.config, appsettings.json
   ```

2. **IIS Status**:
   ```
   - Open IIS Manager
   - Check Application Pool status
   - Verify website bindings
   - Test website from IIS Manager
   ```

3. **Direct API Test**:
   ```
   curl http://202.164.153.160:85/api/test/health
   ```

## Security Considerations

- FTP credentials are stored as GitHub secrets
- Database connection strings are encrypted
- Development configuration files are excluded from deployment
- PDB files and debug symbols are excluded

## Customization

### Changing Server Configuration

Edit the environment variables in the workflow file:

```yaml
env:
  REMOTE_SERVER: '202.164.153.160'  # Your server IP
  REMOTE_PORT: '85'                 # Your HTTP port
  FTP_PORT: '92'                    # Your FTP port
```

### Modifying Deployment Path

Change the `server-dir` in the FTP deploy step:

```yaml
server-dir: /your-path/  # Maps to C:\inetpub\wwwroot\your-path\
```

### Adding Additional Tests

Add steps before the "Publish application" step:

```yaml
- name: Integration Tests
  run: dotnet test IntegrationTests --configuration Release
```

## Support

For issues with this deployment workflow:

1. Check the GitHub Actions logs for detailed error messages
2. Review the troubleshooting section above
3. Verify server configuration matches requirements
4. Test FTP connectivity manually if needed

## Version History

- **v1.0**: Initial deployment workflow
- **v1.1**: Added health checks and better error handling
- **v1.2**: Enhanced with artifact upload and comprehensive logging

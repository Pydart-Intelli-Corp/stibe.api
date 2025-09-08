# IIS Deployment and Logging Guide for Stibe API

## 🚀 Deployment Steps

### 1. Publish the API
```powershell
# Build and publish for production
dotnet publish -c Release -o ./publish

# Copy files to IIS server
# Copy the entire ./publish folder to your IIS server directory
```

### 2. IIS Configuration
```xml
<!-- Ensure web.config is properly configured -->
<!-- File: web.config (already created) -->
<!-- This enables stdout logging and proper ASP.NET Core hosting -->
```

### 3. Directory Permissions
On the IIS server, ensure these directories exist and have proper permissions:
- `wwwroot/uploads/profile-images/` (IIS_IUSRS needs write access)
- `logs/` (IIS_IUSRS needs write access)

## 📊 Log Locations on IIS Server

### Application Logs (Serilog)
- **Location**: `[App Directory]/logs/stibe-api-YYYY-MM-DD.log`
- **Content**: Detailed application logs with profile upload debugging
- **Rotation**: Daily, keeps 7 days

### IIS Stdout Logs
- **Location**: `[App Directory]/logs/stdout_YYYYMMDD_HHMMSS_PROCESSID.log`
- **Content**: Console output from the application
- **Purpose**: Captures startup errors and console logging

### IIS Access Logs
- **Location**: `C:\inetpub\logs\LogFiles\W3SVC1\u_exYYMMDD.log`
- **Content**: HTTP requests, response codes, response times
- **Purpose**: Monitor HTTP traffic and performance

### Windows Event Logs
- **Location**: Windows Event Viewer > Windows Logs > Application
- **Source**: "IIS AspNetCore Module" and "ASP.NET Core"
- **Content**: IIS module errors and application crashes

## 🔍 Monitoring Commands

### Real-time Log Monitoring
```powershell
# Monitor application logs
Get-Content ".\logs\stibe-api-$(Get-Date -Format 'yyyy-MM-dd').log" -Wait -Tail 50

# Monitor IIS stdout logs
Get-Content ".\logs\stdout*.log" -Wait -Tail 50

# Monitor IIS access logs (replace with actual date)
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\u_ex$(Get-Date -Format 'yyMMdd').log" -Wait -Tail 50

# Use the PowerShell monitoring script
.\monitor-logs.ps1 -Follow -ShowUploadLogs
```

### Search for Specific Issues
```powershell
# Search for profile upload errors
Select-String -Path ".\logs\*.log" -Pattern "PROFILE.*IMAGE.*UPLOAD.*ERROR" -Context 2

# Search for 500 errors
Select-String -Path ".\logs\*.log" -Pattern "StatusCode.*500|Internal Server Error" -Context 2

# Search for file permission errors
Select-String -Path ".\logs\*.log" -Pattern "Access.*denied|Permission.*denied|UnauthorizedAccessException" -Context 2

# Search for disk space issues
Select-String -Path ".\logs\*.log" -Pattern "disk.*full|no space|insufficient.*space" -Context 2
```

## 🐛 Debugging Profile Image Upload Issues

### Common Issues and Solutions

#### 1. **500 Internal Server Error**
**Check**: Application logs for detailed error information
```powershell
Select-String -Path ".\logs\*.log" -Pattern "=== PROFILE IMAGE UPLOAD.*ERROR ===" -Context 5
```

#### 2. **File Permission Issues**
**Symptoms**: "Access denied" or "UnauthorizedAccessException"
**Solution**: 
```powershell
# Grant IIS_IUSRS write access to uploads directory
icacls "wwwroot\uploads" /grant "IIS_IUSRS:(OI)(CI)F" /T
```

#### 3. **Directory Not Found**
**Symptoms**: "Directory does not exist" errors
**Solution**: Ensure directories are created
```powershell
New-Item -ItemType Directory -Path "wwwroot\uploads\profile-images" -Force
New-Item -ItemType Directory -Path "logs" -Force
```

#### 4. **WebRootPath Issues**
**Symptoms**: "WebRootPath is null or empty"
**Check**: IIS application configuration and hosting model

### Profile Upload Debug Log Format
Look for these log entries:
```
=== PROFILE IMAGE UPLOAD STARTED ===
User found: user@example.com
Image file details - Name: image.jpg, Size: 127331 bytes
WebRootPath: C:\inetpub\wwwroot\stibe-api
Uploads directory path: C:\inetpub\wwwroot\stibe-api\wwwroot\uploads\profile-images
Generated file name: 123_guid.jpg
File saved successfully to: full-path
Generated image URL: http://202.164.153.160:85/uploads/profile-images/123_guid.jpg
=== PROFILE IMAGE UPLOAD COMPLETED SUCCESSFULLY ===
```

## 📱 Testing from Flutter App

### Test Profile Upload
1. Trigger profile image upload from the Flutter app
2. Check logs immediately:
```powershell
.\monitor-logs.ps1 -ShowUploadLogs
```

### Verify File Creation
```powershell
# Check if files are being created
Get-ChildItem "wwwroot\uploads\profile-images" | Sort-Object LastWriteTime -Descending | Select-Object -First 10
```

### Test Image Access
```powershell
# Test if uploaded images are accessible
Invoke-WebRequest "http://202.164.153.160:85/uploads/profile-images/latest-image.jpg"
```

## 🔧 Advanced Debugging

### Enable Detailed Entity Framework Logging
Add to appsettings.Production.json:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Database.Transaction": "Information"
    }
  }
}
```

### Enable IIS Failed Request Tracing
1. Enable in IIS Manager > Failed Request Tracing Rules
2. Add rule for 500 errors
3. Check `C:\inetpub\logs\FailedReqLogFiles\`

### Performance Monitoring
```powershell
# Monitor response times from IIS logs
Select-String -Path "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Pattern "POST.*profile.*image" | 
    ForEach-Object { $_.Line.Split(' ')[-1] } | Measure-Object -Average
```

## 📞 Support Information

- **API URL**: http://202.164.153.160:85
- **Swagger**: http://202.164.153.160:85/swagger
- **Health Check**: http://202.164.153.160:85/api/test/health

For real-time support, monitor logs and provide specific error messages from the detailed logging.

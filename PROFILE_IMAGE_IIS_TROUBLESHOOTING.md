# Profile Image Upload - IIS Troubleshooting Guide

## Issue Description
Profile photo URLs work when running `dotnet run` locally but fail when hosted on IIS.

## Root Causes

### 1. URL Generation Issues
- **Local**: URLs generated using `Request.Host` work because the development server handles everything
- **IIS**: URLs might not resolve correctly due to reverse proxy setup or different hosting configuration

### 2. Static File Serving Issues  
- **Local**: ASP.NET Core development server automatically serves static files
- **IIS**: Requires proper configuration of static file handlers and MIME types

### 3. File Permissions
- **Local**: Application runs under user context with full permissions
- **IIS**: Application pool runs under restricted identity (IIS_IUSRS)

## Solutions Implemented

### 1. Updated appsettings.Production.json
```json
"FileStorage": {
  "LocalPath": "wwwroot/uploads",
  "BaseUrl": "http://202.164.153.160:85/uploads"
}
```
**Purpose**: Use absolute URLs in production instead of relative paths

### 2. Modified AuthController URL Generation
```csharp
// Generate URL for the file using configured base URL
var configuredBaseUrl = _configuration["FileStorage:BaseUrl"] ?? "/uploads";
string imageUrl;

// If configured base URL is absolute, use it directly
if (configuredBaseUrl.StartsWith("http"))
{
    imageUrl = $"{configuredBaseUrl}/profile-images/{fileName}";
}
else
{
    // Fallback to request-based URL for local development
    var baseUrl = $"{Request.Scheme}://{Request.Host}";
    imageUrl = $"{baseUrl}{configuredBaseUrl}/profile-images/{fileName}";
}
```
**Purpose**: Use configured base URL for production, fallback to request-based for development

### 3. Enhanced web.config
```xml
<!-- Static content handling for uploads -->
<staticContent>
  <mimeMap fileExtension=".jpg" mimeType="image/jpeg" />
  <mimeMap fileExtension=".jpeg" mimeType="image/jpeg" />
  <mimeMap fileExtension=".png" mimeType="image/png" />
</staticContent>

<!-- Configure handlers for uploads directory -->
<location path="uploads">
  <system.webServer>
    <handlers>
      <clear />
      <add name="StaticFileModuleHandler" path="*" verb="*" modules="StaticFileModule" resourceType="Either" requireAccess="Read" />
    </handlers>
  </system.webServer>
</location>
```
**Purpose**: Ensure IIS properly handles static files in uploads directory

### 4. Improved Static File Configuration in Program.cs
```csharp
// Static files for uploads with caching and proper MIME types
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Set cache headers for uploaded files
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
        
        // Ensure proper MIME types for images
        var extension = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                ctx.Context.Response.ContentType = "image/jpeg";
                break;
            case ".png":
                ctx.Context.Response.ContentType = "image/png";
                break;
        }
    }
});
```
**Purpose**: Explicit MIME type handling and caching for better performance

## Deployment Steps

### 1. Update Code
- Deploy updated code with all the changes above
- Ensure appsettings.Production.json has correct BaseUrl

### 2. Run IIS Fix Script
Run `fix-iis-uploads.ps1` on the IIS server to:
- Create necessary directories
- Set proper permissions
- Verify IIS features
- Restart IIS

### 3. Test Upload Functionality
Run `test-uploads.ps1` to:
- Test API connectivity
- Verify static file access
- Check directory structure
- Get manual testing instructions

### 4. Manual Verification
1. Upload a profile image via API
2. Check file is created in `wwwroot/uploads/profile-images/`
3. Access the file directly via URL: `http://yourserver/uploads/profile-images/filename.jpg`
4. Verify the API returns the correct URL

## Common Issues and Solutions

### Issue: 404 when accessing uploaded images
**Cause**: IIS not configured to serve static files from uploads directory
**Solution**: 
- Check web.config has proper static file handlers
- Verify uploads directory exists and has correct permissions
- Run fix-iis-uploads.ps1

### Issue: Upload succeeds but returns relative URL
**Cause**: BaseUrl not configured in appsettings.Production.json
**Solution**: Set absolute URL in FileStorage:BaseUrl

### Issue: 500 error during upload
**Cause**: Insufficient permissions or missing directories
**Solution**: 
- Run fix-iis-uploads.ps1 to set permissions
- Check application logs for specific error details

### Issue: Files upload but URLs are incorrect
**Cause**: AuthController not using configured BaseUrl
**Solution**: Verify AuthController has IConfiguration injected and uses it

## Testing Commands

### Test profile image upload with curl:
```bash
curl -X POST "http://202.164.153.160:85/api/auth/profile/image" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "profileImage=@test-image.jpg"
```

### Test direct file access:
```bash
curl -I "http://202.164.153.160:85/uploads/profile-images/filename.jpg"
```

## Directory Structure
```
wwwroot/
├── uploads/
│   ├── profile-images/
│   ├── shop-images/
│   ├── service-images/
│   └── product-images/
├── css/
├── index.html
└── other static files...
```

## Required IIS Features
- IIS-WebServerRole
- IIS-WebServer
- IIS-CommonHttpFeatures
- IIS-StaticContent
- IIS-NetFxExtensibility45
- IIS-AspNetCoreModule
- IIS-AspNetCoreModuleV2

## File Permissions Required
- IIS_IUSRS: Full Control on uploads directory
- IIS AppPool\DefaultAppPool: Full Control on uploads directory

This should resolve the profile photo URL issues when hosted on IIS.

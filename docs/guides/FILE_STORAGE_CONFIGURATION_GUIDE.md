# 🔄 File Storage Provider Configuration Examples

This document shows different configuration examples for switching between storage providers in Stibe.API.

## 📂 Local Storage (Default)

**Use case**: Development, testing, small-scale deployments

```json
{
  "FileStorage": {
    "Provider": "Local",
    "LocalPath": "wwwroot/uploads",
    "BaseUrl": "/uploads"
  }
}
```

**Features**:
- ✅ No external dependencies
- ✅ Fast local access
- ✅ Simple setup
- ❌ Limited scalability
- ❌ Single server storage

---

## ☁️ Azure Blob Storage

**Use case**: Production, scalable applications, global distribution

```json
{
  "FileStorage": {
    "Provider": "Azure",
    "Azure": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=stibefiles;AccountKey=your_key_here;EndpointSuffix=core.windows.net",
      "ContainerName": "stibe-files",
      "BaseUrl": "https://stibefiles.blob.core.windows.net/stibe-files"
    }
  }
}
```

**Features**:
- ✅ Unlimited scalability
- ✅ Global CDN
- ✅ 99.9% availability
- ✅ Built-in backup/redundancy
- ❌ Requires Azure account
- ❌ Internet connectivity required

---

## 🔄 Hybrid Storage

**Use case**: Maximum reliability with failover capability

```json
{
  "FileStorage": {
    "Provider": "Hybrid",
    "LocalPath": "wwwroot/uploads",
    "BaseUrl": "/uploads",
    "Azure": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=stibefiles;AccountKey=your_key_here;EndpointSuffix=core.windows.net",
      "ContainerName": "stibe-files",
      "BaseUrl": "https://stibefiles.blob.core.windows.net/stibe-files"
    }
  }
}
```

**Features**:
- ✅ Azure primary with local fallback
- ✅ Automatic failover
- ✅ Best of both worlds
- ❌ More complex setup
- ❌ Storage duplication

---

## 🌍 Environment-Specific Examples

### Development Environment
```json
{
  "FileStorage": {
    "Provider": "Local",
    "LocalPath": "wwwroot/uploads",
    "BaseUrl": "/uploads"
  }
}
```

### Staging Environment
```json
{
  "FileStorage": {
    "Provider": "Azure",
    "Azure": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=stibestaging;AccountKey=staging_key;EndpointSuffix=core.windows.net",
      "ContainerName": "stibe-staging-files",
      "BaseUrl": "https://stibestaging.blob.core.windows.net/stibe-staging-files"
    }
  }
}
```

### Production Environment
```json
{
  "FileStorage": {
    "Provider": "Hybrid",
    "LocalPath": "/var/www/stibe/uploads",
    "BaseUrl": "https://api.stibe.app/uploads",
    "Azure": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=stibeprod;AccountKey=production_key;EndpointSuffix=core.windows.net",
      "ContainerName": "stibe-files",
      "BaseUrl": "https://cdn.stibe.app/files"
    }
  }
}
```

---

## 🚀 Quick Switch Instructions

### Step 1: Update Configuration
Edit `appsettings.json` and change the `Provider` value:

```json
{
  "FileStorage": {
    "Provider": "Azure"  // Change this value
  }
}
```

### Step 2: Restart Application
The application needs to be restarted for the new provider to take effect.

### Step 3: Verify Switch
Use the test endpoint to confirm the switch:

```http
GET /api/FileStorageTest/status
```

---

## 🧪 Testing Different Providers

### Test Current Provider
```http
POST /api/FileStorageTest/test-upload
Content-Type: multipart/form-data

file: [test image]
```

### Check Status
```http
GET /api/FileStorageTest/status
```

### Upload Profile Image (Real endpoint)
```http
POST /api/FileUpload/profile-image
Authorization: Bearer your_jwt_token
Content-Type: multipart/form-data

file: [profile image]
```

---

## ⚠️ Important Notes

1. **Application Restart Required**: Changing the provider requires restarting the application
2. **Data Migration**: Files uploaded with one provider aren't automatically available in another
3. **URL Compatibility**: File URLs will differ between providers
4. **Configuration Validation**: Ensure all required settings are present for the chosen provider

---

## 🔧 Troubleshooting Provider Switch

### Issue: Files Not Found After Switch
**Problem**: Uploaded files with one provider are not accessible after switching.

**Solution**: Files remain in their original storage location. URLs change between providers.

### Issue: Azure Connection Fails
**Problem**: Application fails to start with Azure provider.

**Solution**: 
- Verify connection string
- Check Azure storage account status
- Ensure container exists

### Issue: Local Storage Permission Denied
**Problem**: Cannot write to local storage directory.

**Solution**:
- Check directory permissions
- Ensure application has write access
- Verify directory path exists

---

## 📊 Provider Comparison

| Feature | Local | Azure | Hybrid |
|---------|-------|-------|--------|
| **Setup Complexity** | Simple | Medium | Complex |
| **Scalability** | Limited | Unlimited | High |
| **Cost** | Storage only | Usage-based | Combined |
| **Reliability** | Single point | 99.9% SLA | Highest |
| **Speed** | Fastest | Fast | Fast |
| **Internet Dependency** | None | Required | Optional |
| **Global Access** | Limited | Global | Global |

---

## 📞 Support

If you encounter issues while switching providers:

1. Check application logs for detailed error messages
2. Verify configuration syntax in `appsettings.json`
3. Test connectivity to Azure (if using Azure provider)
4. Use the `/api/FileStorageTest/status` endpoint for diagnostics
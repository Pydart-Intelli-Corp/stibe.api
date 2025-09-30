# 🗄️ Azure Blob Storage Integration Guide

This guide explains how to use Azure Blob Storage with the Stibe.API file management system.

## 🚀 Quick Start

### 1. Choose Your Storage Provider

Update `appsettings.json` to select your file storage provider:

```json
{
  "FileStorage": {
    "Provider": "Azure",  // Options: "Local", "Azure", "Hybrid"
    "LocalPath": "wwwroot/uploads",
    "BaseUrl": "/uploads",
    "Azure": {
      "ConnectionString": "your_azure_storage_connection_string_here",
      "ContainerName": "stibe-files",
      "BaseUrl": "https://yourstorageaccount.blob.core.windows.net/stibe-files"
    }
  }
}
```

### 2. Provider Options

| Provider | Description | Use Case |
|----------|-------------|----------|
| **Local** | Files stored on server disk | Development, small scale |
| **Azure** | Files stored in Azure Blob Storage | Production, scalability |
| **Hybrid** | Azure primary, Local fallback | Maximum reliability |

## ⚙️ Azure Storage Setup

### Step 1: Create Azure Storage Account

1. Go to [Azure Portal](https://portal.azure.com)
2. Create new **Storage Account**
3. Choose **Standard** performance tier
4. Select **Hot** access tier for frequently accessed files
5. Enable **Public access** for blob containers

### Step 2: Get Connection String

1. Go to your Storage Account
2. Navigate to **Access keys**
3. Copy **Connection string** from key1 or key2

### Step 3: Create Container

The application will automatically create the container, but you can manually create it:

1. Go to **Containers** in your Storage Account
2. Click **+ Container**
3. Name: `stibe-files` (or your configured name)
4. Public access level: **Blob** (for direct URL access)

### Step 4: Update Configuration

Replace the placeholder in `appsettings.json`:

```json
{
  "FileStorage": {
    "Provider": "Azure",
    "Azure": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=your_actual_key_here;EndpointSuffix=core.windows.net",
      "ContainerName": "stibe-files",
      "BaseUrl": "https://youraccount.blob.core.windows.net/stibe-files"
    }
  }
}
```

## 🔧 File Organization

Files are organized in the following structure:

```
Azure Container: stibe-files/
├── profile-images/
│   ├── user123_20241001123045_a1b2c3d4.jpg
│   └── user456_20241001123156_e5f6g7h8.png
├── shop-images/
│   ├── shop001_20241001123245_i9j0k1l2.jpg
│   └── gallery_20241001123345_m3n4o5p6.webp
├── staff-images/
│   └── staff789_20241001123445_q7r8s9t0.jpg
├── service-images/
│   └── service101_20241001123545_u1v2w3x4.png
└── test-uploads/
    └── test_files_from_api_testing.jpg
```

## 📡 API Usage

### Check Storage Status

```http
GET /api/FileStorageTest/status
Authorization: Bearer your_jwt_token
```

Response:
```json
{
  "success": true,
  "data": {
    "currentProvider": "AZURE",
    "configuration": {
      "provider": "Azure Blob Storage",
      "containerName": "stibe-files",
      "baseUrl": "https://youraccount.blob.core.windows.net/stibe-files",
      "hasConnectionString": true
    },
    "availableProviders": ["Local", "Azure", "Hybrid"]
  }
}
```

### Test File Upload

```http
POST /api/FileStorageTest/test-upload
Authorization: Bearer your_jwt_token
Content-Type: multipart/form-data

file: [binary file data]
```

Response:
```json
{
  "success": true,
  "data": {
    "success": true,
    "fileUrl": "https://youraccount.blob.core.windows.net/stibe-files/test-uploads/image_20241001123456_a1b2c3d4.jpg",
    "provider": "AZURE",
    "originalFileName": "image.jpg",
    "fileSize": 245760,
    "uploadDurationMs": 1250.5
  },
  "message": "File uploaded successfully"
}
```

### Upload Profile Image (Existing Endpoint)

```http
POST /api/FileUpload/profile-image
Authorization: Bearer your_jwt_token
Content-Type: multipart/form-data

file: [image file]
```

## 🔄 Provider Switching

You can switch between providers by updating the configuration:

### Switch to Local Storage
```json
{
  "FileStorage": {
    "Provider": "Local"
  }
}
```

### Switch to Azure Storage
```json
{
  "FileStorage": {
    "Provider": "Azure"
  }
}
```

### Switch to Hybrid Mode
```json
{
  "FileStorage": {
    "Provider": "Hybrid"
  }
}
```

**Note:** Application restart is required after changing providers.

## 🛡️ Security Features

### 1. Automatic Content Type Detection
Files are assigned proper MIME types based on extension:
- `.jpg/.jpeg` → `image/jpeg`
- `.png` → `image/png`
- `.gif` → `image/gif`
- `.webp` → `image/webp`
- `.pdf` → `application/pdf`

### 2. Unique File Naming
Files are renamed to prevent conflicts:
```
original.jpg → original_20241001123456_a1b2c3d4.jpg
```

### 3. Metadata Storage
Each blob includes metadata:
- Original filename
- Upload timestamp
- Container classification

### 4. Public Access Control
Blobs are configured for public read access via direct URLs.

## 📊 Cost Optimization

### Azure Blob Storage Pricing (Approximate)

| Storage Type | Price (USD/GB/month) | Use Case |
|--------------|---------------------|----------|
| **Hot** | $0.0184 | Frequently accessed files |
| **Cool** | $0.0100 | Infrequently accessed files |
| **Archive** | $0.00099 | Rarely accessed files |

### Transaction Costs
- **Write operations**: $0.0065 per 10,000 transactions
- **Read operations**: $0.0004 per 10,000 transactions

### Bandwidth
- **Outbound data transfer**: $0.087/GB (first 100TB)

### Cost Estimation for Stibe
Assuming:
- 1000 shops × 10 images each = 10,000 images
- Average image size: 500KB
- Total storage: ~5GB
- Monthly reads: 50,000

**Monthly Cost**: ~$0.09 + $0.002 + $4.35 = **~$4.44/month**

## 🚨 Troubleshooting

### Common Issues

#### 1. Connection String Invalid
**Error**: `The remote server returned an error: (403) Forbidden`

**Solution**: 
- Verify connection string in Azure Portal
- Check storage account access keys
- Ensure account name and key are correct

#### 2. Container Not Found
**Error**: `The specified container does not exist`

**Solution**:
- Application creates container automatically
- Manually create container in Azure Portal
- Check container name in configuration

#### 3. Blob Not Found for Deletion
**Warning**: `Blob not found for deletion`

**Solution**:
- Normal behavior for non-existent files
- Check URL format and container name
- Verify blob exists in Azure Portal

#### 4. Upload Timeout
**Error**: `The operation was canceled`

**Solution**:
- Check internet connectivity
- Verify Azure service status
- Increase timeout in HttpClient configuration

### Health Check Endpoint

Monitor storage health:
```http
GET /api/health
```

### Logging

Check application logs for detailed error information:
```
[ERROR] === AZURE BLOB SERVICE - ERROR === Exception during file upload
```

## 🔧 Development Tips

### 1. Local Development with Azure
Use Azure Storage Emulator (Azurite) for local development:

```json
{
  "FileStorage": {
    "Provider": "Azure",
    "Azure": {
      "ConnectionString": "UseDevelopmentStorage=true",
      "ContainerName": "stibe-files",
      "BaseUrl": "http://127.0.0.1:10000/devstoreaccount1/stibe-files"
    }
  }
}
```

### 2. Environment-Specific Configuration

Use environment variables for sensitive data:

```json
{
  "FileStorage": {
    "Azure": {
      "ConnectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
      "ContainerName": "${AZURE_STORAGE_CONTAINER_NAME:stibe-files}"
    }
  }
}
```

### 3. Testing Different Providers

Use the test endpoints to verify functionality:
1. Test upload with current provider
2. Switch provider in configuration
3. Restart application
4. Test upload again

## 📈 Monitoring & Analytics

### Key Metrics to Monitor

1. **Upload Success Rate**
2. **Upload Duration**
3. **Storage Usage**
4. **Bandwidth Consumption**
5. **Error Rates**

### Azure Monitor Integration

Enable diagnostics in Azure Storage Account:
1. Go to **Diagnostic settings**
2. Add diagnostic setting
3. Enable **Blob**, **Queue**, **Table**, **File** logs
4. Send to Log Analytics workspace

## 🎯 Best Practices

### 1. Naming Conventions
- Use consistent container names
- Include timestamps in filenames
- Avoid special characters

### 2. Security
- Use managed identities in production
- Implement CORS policies
- Enable Azure Defender for Storage

### 3. Performance
- Use CDN for global distribution
- Implement client-side caching
- Optimize image sizes before upload

### 4. Backup Strategy
- Enable soft delete for blobs
- Set up geo-redundant storage
- Regular backup validation

---

## 📞 Support

For issues with Azure integration:
1. Check application logs
2. Verify Azure Portal settings
3. Test with `/api/FileStorageTest/` endpoints
4. Review configuration in `appsettings.json`

**Note**: This integration maintains full backward compatibility with existing file upload endpoints. All existing functionality continues to work with the new storage provider.
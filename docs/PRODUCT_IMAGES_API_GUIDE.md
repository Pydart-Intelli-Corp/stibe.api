# Product Images API Guide

## Overview
This guide documents the updated API endpoints to support the new Product Images feature for shop services.

## What's New
- **ProductImages Field**: Added to all service models and DTOs
- **Product Image Upload Endpoint**: Dedicated endpoint for uploading product/tool images
- **Enhanced Service Management**: All service operations now support product images

## Updated Endpoints

### 1. Get Shop Services
**GET** `/api/shop/{shopId}/services`

**New Response Fields:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Hair Cut",
      "description": "Professional hair cutting service",
      "price": 500.00,
      "serviceImages": ["url1", "url2"],
      "productImages": ["product_url1", "product_url2"],
      // ... other fields
    }
  ]
}
```

### 2. Create Service
**POST** `/api/shop/{shopId}/services`

**New Request Fields:**
```json
{
  "name": "Hair Cut",
  "description": "Professional hair cutting service",
  "price": 500.00,
  "serviceImages": ["service_url1", "service_url2"],
  "productImages": ["product_url1", "product_url2"],
  // ... other fields
}
```

### 3. Update Service
**PUT** `/api/shop/{shopId}/services/{serviceId}`

**New Request Fields:**
```json
{
  "name": "Updated Hair Cut",
  "productImages": ["new_product_url1", "new_product_url2"],
  // ... other optional fields
}
```

### 4. Upload Product Images (NEW)
**POST** `/api/shop/{shopId}/services/{serviceId}/upload-product-images`

**Request:** Form-data with image files
**Content-Type:** `multipart/form-data`

**Parameters:**
- `images`: List of image files (max 6 files)
- **File Types**: JPG, PNG, WebP
- **File Size**: Max 5MB per image

**Response:**
```json
{
  "success": true,
  "message": "Product images uploaded successfully",
  "data": {
    "imageUrls": [
      "https://storage.example.com/product-images/product-123-uuid.jpg",
      "https://storage.example.com/product-images/product-123-uuid2.jpg"
    ]
  }
}
```

**Validation:**
- Maximum 6 product images per service
- Only JPG, PNG, WebP formats allowed
- Files must be smaller than 5MB
- Requires shop ownership verification

### 5. Upload Service Images (Enhanced)
**POST** `/api/shop/{shopId}/services/{serviceId}/upload-images`

This existing endpoint continues to work for service images, separate from product images.

## Database Changes

### Services Table
New column added:
```sql
ALTER TABLE `Services` ADD `ProductImages` TEXT NULL;
```

The `ProductImages` column stores a JSON array of image URLs:
```json
["https://storage.example.com/product-images/product-1.jpg", "https://storage.example.com/product-images/product-2.jpg"]
```

## Data Flow

### 1. Create Service with Product Images
```
Frontend → API → Database
1. Service creation with productImages array
2. Images stored as JSON in ProductImages column
3. Service returned with productImages in response
```

### 2. Upload Product Images
```
Frontend → API → File Storage → Database
1. Upload image files to storage
2. Get image URLs from storage service
3. Append URLs to existing ProductImages JSON
4. Update service in database
5. Return uploaded image URLs
```

### 3. Retrieve Services
```
Database → API → Frontend
1. Fetch services from database
2. Deserialize ProductImages JSON to array
3. Return services with productImages field
```

## Integration Examples

### Flutter Integration
```dart
// Create service with product images
final request = CreateServiceRequest(
  name: "Hair Cut",
  description: "Professional service",
  price: 500.00,
  productImages: [
    "https://storage.example.com/product-1.jpg",
    "https://storage.example.com/product-2.jpg"
  ],
);

// Upload product images
final images = await ImagePicker().pickMultiImage();
final response = await serviceApi.uploadProductImages(shopId, serviceId, images);
```

### API Client Example
```javascript
// Create service
const serviceData = {
  name: "Hair Cut",
  description: "Professional service",
  price: 500.00,
  productImages: ["url1", "url2"]
};

fetch(`/api/shop/${shopId}/services`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(serviceData)
});

// Upload product images
const formData = new FormData();
images.forEach(image => formData.append('images', image));

fetch(`/api/shop/${shopId}/services/${serviceId}/upload-product-images`, {
  method: 'POST',
  body: formData
});
```

## Error Handling

### Common Error Responses
```json
{
  "success": false,
  "message": "Maximum 6 product images allowed",
  "errors": []
}
```

### Error Codes
- **400 Bad Request**: Invalid file type, file too large, too many images
- **401 Unauthorized**: Invalid authentication token
- **404 Not Found**: Shop or service not found
- **500 Internal Server Error**: Server-side error

## Security
- **Authentication**: JWT token required
- **Authorization**: Shop ownership verification
- **File Validation**: Type and size validation
- **Input Sanitization**: JSON data validation

## Performance
- **Image Optimization**: Files compressed and optimized
- **Storage**: Efficient cloud storage with CDN
- **Database**: JSON storage for flexible image URLs
- **Caching**: Response caching where appropriate

## Migration Guide

### For Existing Services
1. Existing services will have `productImages` as empty array
2. No data migration required
3. Product images can be added through update operations

### Backward Compatibility
- All existing endpoints remain functional
- New `productImages` field is optional
- Legacy clients will receive empty array for product images

## Testing

### Manual Testing
1. Create service with product images
2. Upload product images to existing service
3. Verify image limits (max 6)
4. Test file type validation
5. Test file size validation

### API Testing
```bash
# Create service with product images
curl -X POST "/api/shop/1/services" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"name":"Test Service","price":100,"productImages":["url1"]}'

# Upload product images
curl -X POST "/api/shop/1/services/1/upload-product-images" \
  -H "Authorization: Bearer {token}" \
  -F "images=@product1.jpg" \
  -F "images=@product2.jpg"
```

## Monitoring
- **File Upload Metrics**: Track upload success/failure rates
- **Storage Usage**: Monitor storage consumption
- **API Performance**: Response time monitoring
- **Error Rates**: Track validation and server errors

This completes the Product Images API implementation, providing full support for managing product/tool images in shop services.
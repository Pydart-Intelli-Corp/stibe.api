# OTP Service Documentation

## Overview

The Stibe API now includes a comprehensive OTP (One-Time Password) service that provides secure email-based verification for various purposes throughout the application.

## Features

### ✅ **Implemented Features**

1. **Multi-Purpose OTP Support**
   - Email Verification
   - Shop Access Control
   - Password Reset
   - Phone Verification
   - Two-Factor Authentication

2. **Security Features**
   - Rate limiting (max 5 OTPs per hour)
   - Attempt limits (3 attempts per OTP)
   - Automatic expiration (10 minutes)
   - IP and User-Agent tracking
   - Secure random code generation

3. **Email Integration**
   - Professional HTML email templates
   - Purpose-specific messaging
   - Clear expiration warnings
   - Security best practices

4. **Database Management**
   - Automatic cleanup of expired OTPs
   - Efficient indexing for performance
   - Audit trail with timestamps

## API Endpoints

### Base URL: `https://your-api-domain.com/api/otp`

### 1. Send OTP
**POST** `/send`

Generates and sends a 6-digit OTP to the specified email address.

```json
{
  "email": "user@example.com",
  "purpose": "SHOP_ACCESS"
}
```

**Response:**
```json
{
  "success": true,
  "message": "OTP sent successfully",
  "data": {
    "success": true,
    "message": "OTP sent successfully",
    "expiresAt": "2025-08-25T17:14:00Z",
    "attemptsRemaining": 3
  }
}
```

### 2. Verify OTP
**POST** `/verify`

Verifies the provided OTP code for the specified email and purpose.

```json
{
  "email": "user@example.com",
  "code": "123456",
  "purpose": "SHOP_ACCESS"
}
```

**Response:**
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "data": {
    "success": true,
    "message": "OTP verified successfully"
  }
}
```

### 3. Get OTP Status
**GET** `/status?email=user@example.com&purpose=SHOP_ACCESS`

Retrieves the current status of OTPs for the specified email and purpose.

**Response:**
```json
{
  "success": true,
  "message": "OTP status retrieved successfully",
  "data": {
    "hasPendingOtp": true,
    "purpose": "SHOP_ACCESS",
    "expiresAt": "2025-08-25T17:14:00Z",
    "attemptsRemaining": 2,
    "nextAllowedAt": "2025-08-25T17:06:00Z",
    "canRequestNew": false
  }
}
```

### 4. Invalidate OTPs
**POST** `/invalidate`

Marks all pending OTPs as used for the specified email and purpose.

```json
{
  "email": "user@example.com",
  "purpose": "SHOP_ACCESS"
}
```

### 5. Cleanup Expired OTPs (Admin Only)
**POST** `/cleanup`

Removes expired OTP records from the database. Requires Admin/SuperAdmin role.

### 6. Get Supported Purposes
**GET** `/purposes`

Returns all supported OTP purposes.

## OTP Purposes

| Purpose | Description | Use Case |
|---------|-------------|----------|
| `EMAIL_VERIFICATION` | Email address verification | User registration |
| `SHOP_ACCESS` | Shop editing access control | Secure shop management |
| `PASSWORD_RESET` | Password reset verification | Account recovery |
| `PHONE_VERIFICATION` | Phone number verification | Contact verification |
| `TWO_FACTOR_AUTH` | Two-factor authentication | Enhanced security |

## Rate Limiting

- **Per Email/Purpose**: Maximum 5 OTPs per hour
- **Between Requests**: 2-minute cooldown between OTP requests
- **Verification Attempts**: 3 attempts per OTP code
- **HTTP Status**: 429 (Too Many Requests) when rate limited

## Security Considerations

### Code Generation
- Uses cryptographically secure random number generation
- 6-digit numeric codes (000000-999999)
- No predictable patterns

### Data Protection
- Email addresses stored in lowercase
- IP addresses and User-Agent strings logged for security
- Automatic invalidation of previous OTPs when new ones are sent

### Expiration
- OTPs expire after 10 minutes
- Used OTPs cannot be reused
- Automatic cleanup of old records

## Email Templates

The service sends professional HTML emails with:
- Clear OTP code display
- Purpose-specific messaging
- Expiration warnings
- Security best practices
- Branded Stibe styling

## Database Schema

### OtpEntity Table
```sql
CREATE TABLE OtpEntities (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Email VARCHAR(100) NOT NULL,
    Code VARCHAR(6) NOT NULL,
    Purpose VARCHAR(50) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    IsUsed BOOLEAN DEFAULT FALSE,
    UsedAt DATETIME NULL,
    IpAddress VARCHAR(45) NULL,
    UserAgent VARCHAR(500) NULL,
    AttemptCount INT DEFAULT 0,
    LastAttemptAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    IsDeleted BOOLEAN DEFAULT FALSE
);
```

## Integration Examples

### Flutter Integration

```dart
// Send OTP for shop access
Future<bool> sendShopAccessOtp(String email) async {
  final response = await http.post(
    Uri.parse('$apiBaseUrl/api/otp/send'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'email': email,
      'purpose': 'SHOP_ACCESS'
    }),
  );
  
  if (response.statusCode == 200) {
    final data = jsonDecode(response.body);
    return data['success'] ?? false;
  }
  return false;
}

// Verify OTP
Future<bool> verifyShopAccessOtp(String email, String code) async {
  final response = await http.post(
    Uri.parse('$apiBaseUrl/api/otp/verify'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'email': email,
      'code': code,
      'purpose': 'SHOP_ACCESS'
    }),
  );
  
  if (response.statusCode == 200) {
    final data = jsonDecode(response.body);
    return data['success'] ?? false;
  }
  return false;
}
```

### Error Handling

```dart
try {
  final success = await sendShopAccessOtp(email);
  if (!success) {
    // Handle rate limiting or other errors
    showSnackBar('Please wait before requesting another OTP');
  }
} catch (e) {
  showSnackBar('Failed to send OTP. Please try again.');
}
```

## Configuration

### SMTP Settings (appsettings.json)
```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Stibe Booking",
    "EnableSSL": true
  }
}
```

### Feature Flags
```json
{
  "FeatureFlags": {
    "UseRealEmailService": true
  }
}
```

## Monitoring and Maintenance

### Recommended Monitoring
- OTP success/failure rates
- Rate limiting triggers
- Email delivery failures
- Database cleanup frequency

### Periodic Tasks
- Run cleanup endpoint weekly
- Monitor email sending quotas
- Review security logs

## Testing

### Manual Testing Endpoints
1. **Send OTP**: Test with valid/invalid emails
2. **Verify OTP**: Test with correct/incorrect codes
3. **Rate Limiting**: Send multiple requests quickly
4. **Expiration**: Wait 10+ minutes and try verification

### Example Test Data
```bash
# Send OTP
curl -X POST "http://localhost:5074/api/otp/send" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","purpose":"SHOP_ACCESS"}'

# Verify OTP
curl -X POST "http://localhost:5074/api/otp/verify" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","code":"123456","purpose":"SHOP_ACCESS"}'
```

## Troubleshooting

### Common Issues

1. **Email Not Received**
   - Check SMTP configuration
   - Verify sender email authentication
   - Check spam folder

2. **Rate Limiting**
   - Wait for cooldown period
   - Check hourly limit

3. **Invalid Code**
   - Ensure code is exactly 6 digits
   - Check if OTP has expired
   - Verify attempts remaining

### Logs
The service provides detailed logging for:
- OTP generation and sending
- Verification attempts
- Rate limiting events
- Email sending failures

## Security Best Practices

1. **Use HTTPS** for all OTP-related requests
2. **Implement client-side rate limiting** to improve UX
3. **Clear OTP inputs** after successful verification
4. **Show appropriate error messages** without revealing system details
5. **Log security events** for monitoring

---

## 🎉 Ready for Production

The OTP service is now fully implemented and ready for use in the Stibe application. It provides enterprise-grade security features while maintaining ease of use for both developers and end users.

For integration with the Flutter app's shop editing functionality, simply replace the placeholder OTP sending logic with calls to this API service.

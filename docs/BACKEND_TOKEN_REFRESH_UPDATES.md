# Backend API Updates for Proactive Token Refresh

## Overview
Updated the Stibe API backend to better support the new proactive token refresh behavior implemented in the Flutter app.

## Changes Made

### 1. Enhanced JWT Configuration (`appsettings.json`)
Added new JWT settings to support proactive refresh:
```json
"JwtSettings": {
    "SecretKey": "production-super-secret-key...",
    "Issuer": "StibeAPI",
    "Audience": "StibeUsers", 
    "ExpiryInMinutes": 60,
    "RefreshTokenExpiryInDays": 30,           // NEW: Refresh token validity
    "EnableProactiveRefresh": true,           // NEW: Enable proactive refresh
    "ProactiveRefreshThresholdMinutes": 10    // NEW: When to trigger refresh
}
```

### 2. Updated JwtSettings Class (`JwtSettings.cs`)
Extended the configuration class with new properties:
- `RefreshTokenExpiryInDays`: Controls refresh token lifetime (30 days)
- `EnableProactiveRefresh`: Feature flag for proactive refresh behavior
- `ProactiveRefreshThresholdMinutes`: Time threshold for triggering refresh (10 minutes)

### 3. Enhanced Refresh Token Endpoint (`AuthController.cs`)
Improved the `/auth/refresh-token` endpoint:
- **Better Logging**: Added detailed logging for token refresh attempts
- **Enhanced Error Messages**: More specific error responses
- **User Validation**: Better user existence and status checking
- **Security Logging**: Log both successful and failed refresh attempts

### 4. New Token Status Endpoint
Added `/auth/token-status` GET endpoint:
- **Purpose**: Allow clients to check token validity without refreshing
- **Authorization**: Requires valid bearer token
- **Response**: Returns token validity status and user info
- **Use Case**: Health checks and token validation

## API Endpoints

### Existing (Enhanced)
- `POST /auth/refresh-token` - Refresh expired/expiring tokens
  - Enhanced logging and error handling
  - Better user validation

### New
- `GET /auth/token-status` - Check token validity
  - Returns token status without refresh
  - Useful for health checks

## Client-Side Integration

The backend now supports the Flutter app's proactive refresh strategy:

1. **Token Lifetime**: 60 minutes (as before)
2. **Refresh Window**: 10 minutes before expiry (configurable)
3. **Refresh Token Validity**: 30 days
4. **Proactive Behavior**: App refreshes tokens automatically
5. **No Auto-Logout**: Backend supports keeping users logged in

## Configuration Benefits

### For Development
- Set `EnableProactiveRefresh: false` to disable proactive behavior
- Adjust `ProactiveRefreshThresholdMinutes` for different refresh timings

### For Production
- Extended refresh token validity (30 days) reduces login frequency
- Better logging helps monitor token refresh patterns
- Configurable thresholds allow fine-tuning of refresh behavior

## Security Considerations

1. **Refresh Token Security**: 30-day validity balanced with security
2. **Enhanced Logging**: Better audit trail for token operations
3. **Status Checks**: Allow token validation without exposing sensitive data
4. **Error Handling**: Detailed logging while keeping client errors generic

## Monitoring and Debugging

The enhanced logging will help monitor:
- Token refresh frequency and success rates
- Failed refresh attempts and reasons
- Token status check patterns
- User authentication health

## Backward Compatibility

All changes are backward compatible:
- Existing refresh token endpoint works as before
- New configuration has sensible defaults
- Optional features can be disabled via configuration

## Future Enhancements

The configuration structure supports future additions:
- Custom refresh intervals per user role
- Token refresh rate limiting
- Advanced security policies
- Metrics and analytics integration

## Testing Recommendations

1. **Token Refresh Flow**: Test automatic refresh before expiry
2. **Token Status**: Verify status endpoint accuracy
3. **Error Handling**: Test invalid token scenarios
4. **Logging**: Verify log entries for all token operations
5. **Configuration**: Test with different threshold values

This backend update provides a solid foundation for the Flutter app's new "no auto-logout" behavior while maintaining security and providing better monitoring capabilities.
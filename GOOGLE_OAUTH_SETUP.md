# Google OAuth Integration Setup Guide

This guide explains how to configure and use Google OAuth authentication in the Stibe API.

## Overview

Google OAuth has been successfully integrated into your Stibe API. This allows users to register and login using their Google accounts, providing a seamless authentication experience.

## Features Added

### 1. New API Endpoints

#### Google Login
- **Endpoint**: `POST /api/auth/google-login`
- **Purpose**: Authenticate existing users or auto-register new users via Google
- **Request Body**:
```json
{
  "googleToken": "string", // Google ID token from client
  "role": "Customer", // "Customer" or "SalonOwner"
  "acceptTerms": true
}
```

#### Google Register
- **Endpoint**: `POST /api/auth/google-register`
- **Purpose**: Explicitly register new users via Google
- **Request Body**:
```json
{
  "googleToken": "string", // Google ID token from client
  "role": "Customer", // "Customer" or "SalonOwner"
  "acceptTerms": true
}
```

### 2. Enhanced User Model
- Added `GoogleId` field to store Google user identifier
- Enhanced profile picture support for Google profile images
- Automatic email verification for Google users

### 3. Google OAuth Service
- Token validation using Google's JWT verification
- User information extraction from Google tokens
- Secure integration with existing JWT authentication

## Configuration Setup

### 1. Google Cloud Console Setup

1. **Create a Google Cloud Project**:
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select existing one

2. **Enable Google+ API**:
   - Navigate to APIs & Services > Library
   - Search for "Google+ API" and enable it

3. **Create OAuth 2.0 Credentials**:
   - Go to APIs & Services > Credentials
   - Click "Create Credentials" > "OAuth 2.0 Client IDs"
   - Select "Web application"
   - Add authorized redirect URIs:
     - `https://localhost:7000/api/auth/google-callback` (Development)
     - `https://your-production-domain.com/api/auth/google-callback` (Production)
   - Note down the Client ID and Client Secret

### 2. API Configuration

Update your `appsettings.json` and `appsettings.Development.json`:

```json
{
  "GoogleOAuth": {
    "ClientId": "your-google-client-id-here",
    "ClientSecret": "your-google-client-secret-here",
    "RedirectUri": "https://localhost:7000/api/auth/google-callback",
    "Enabled": true
  }
}
```

**Important**: Replace the placeholder values with your actual Google OAuth credentials.

### 3. Database Migration

After making the User model changes, run the migration:

```bash
# Stop the application first if it's running
dotnet ef migrations add AddGoogleIdToUser
dotnet ef database update
```

## Client Integration

### Flutter/Mobile Integration

For your Flutter apps (stibe_one, stibe_partner, stibe_control), you'll need to:

1. **Add Google Sign-In Package**:
```yaml
dependencies:
  google_sign_in: ^6.1.5
```

2. **Configure Google Sign-In**:
```dart
// Example implementation
import 'package:google_sign_in/google_sign_in.dart';

class GoogleAuthService {
  static const GoogleSignIn _googleSignIn = GoogleSignIn(
    scopes: ['email', 'profile'],
  );

  static Future<String?> signInWithGoogle() async {
    try {
      final GoogleSignInAccount? account = await _googleSignIn.signIn();
      if (account != null) {
        final GoogleSignInAuthentication auth = await account.authentication;
        return auth.idToken; // This is the token to send to your API
      }
    } catch (error) {
      print('Google sign in error: $error');
    }
    return null;
  }
}
```

3. **Update AuthProvider** (for stibe_one):
```dart
// Add this method to your AuthProvider
Future<bool> googleLogin(String googleToken, {String role = "Customer"}) async {
  _setLoading(true);
  _clearError();

  try {
    final response = await _authService.googleLogin(googleToken, role);
    if (response.success) {
      _currentUser = response.user;
      _isAuthenticated = true;
      notifyListeners();
      return true;
    } else {
      _setError(response.message);
      return false;
    }
  } catch (e) {
    _setError(_getErrorMessage(e));
    return false;
  } finally {
    _setLoading(false);
  }
}
```

### Web Integration

For web applications, use Google's JavaScript library:

```html
<script src="https://accounts.google.com/gsi/client" async defer></script>
```

```javascript
function handleCredentialResponse(response) {
  // response.credential contains the Google ID token
  // Send this to your API's /api/auth/google-login endpoint
  fetch('/api/auth/google-login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      googleToken: response.credential,
      role: 'Customer'
    })
  })
  .then(response => response.json())
  .then(data => {
    if (data.success) {
      // Store JWT token and redirect user
      localStorage.setItem('token', data.data.token);
      window.location.href = '/dashboard';
    }
  });
}
```

## Security Considerations

1. **Token Validation**: All Google tokens are validated server-side using Google's official library
2. **Email Verification**: Users signed in via Google have automatic email verification
3. **Unique Constraints**: Email uniqueness is maintained across traditional and Google sign-ups
4. **Profile Data**: Google profile pictures and basic info are securely stored

## Testing

### Using Swagger/Postman

1. Get a Google ID token from a client application
2. Send POST request to `/api/auth/google-login` with the token
3. Verify you receive a JWT token in response
4. Use the JWT token for subsequent API calls

### Example Successful Response

```json
{
  "success": true,
  "message": "Welcome back, John!",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64-encoded-refresh-token",
    "expiresAt": "2025-07-27T14:30:00Z",
    "user": {
      "id": 123,
      "firstName": "John",
      "lastName": "Doe",
      "email": "john.doe@gmail.com",
      "phoneNumber": "",
      "role": "Customer",
      "isEmailVerified": true,
      "profilePictureUrl": "https://lh3.googleusercontent.com/..."
    }
  }
}
```

## Troubleshooting

### Common Issues

1. **Invalid Google Token**:
   - Ensure your Google Client ID matches the one configured in the API
   - Check token expiration
   - Verify the token was generated for the correct audience

2. **User Already Exists Error**:
   - Use `/api/auth/google-login` instead of `/api/auth/google-register`
   - Google login automatically handles existing users

3. **Configuration Issues**:
   - Verify Google OAuth settings in appsettings.json
   - Ensure GoogleOAuth.Enabled is set to true
   - Check Google Cloud Console credentials

### Logging

Monitor the application logs for Google OAuth specific information:
- Google login attempts
- Token validation results
- User creation/updates
- Error details

## Next Steps

1. **Configure Google Cloud Console** with your actual domain and credentials
2. **Update configuration files** with real Google OAuth credentials
3. **Run database migration** to add GoogleId field
4. **Test the integration** with a real Google account
5. **Update your Flutter apps** to integrate Google Sign-In
6. **Deploy and test** in production environment

## Support

If you encounter any issues:
1. Check the application logs for detailed error messages
2. Verify Google Cloud Console configuration
3. Ensure all NuGet packages are properly restored
4. Test with different Google accounts to verify functionality

The Google OAuth integration is now ready for use and provides a modern, secure authentication option for your users!

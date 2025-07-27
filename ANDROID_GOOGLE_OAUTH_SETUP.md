# Google OAuth Android Setup Guide

## Overview
This guide explains how to set up Google OAuth authentication for Android applications in the Stibe booking system.

## Current Configuration

### API Configuration
Your .NET API is now configured to support multiple client types:
- **Web Client ID**: `986486622148-0811nmnfmnjmnc0er554rvlqpn6dlvpl.apps.googleusercontent.com`
- **Android Client ID**: `986486622148-0811nmnfmnjmnc0er554rvlqpn6dlvpl.apps.googleusercontent.com`
- **Project ID**: `stibe-booking-app`

### API Endpoints Available
- `POST /api/auth/google-login` - Login with Google
- `POST /api/auth/google-register` - Register with Google

## Android Application Configuration

### 1. Update Flutter Android Configuration

For each Flutter app (`stibe_one`, `stibe_partner`, `stibe_control`), you need to:

#### A. Add google-services.json
Create `android/app/google-services.json` with:
```json
{
  "project_info": {
    "project_number": "986486622148",
    "project_id": "stibe-booking-app",
    "storage_bucket": "stibe-booking-app.appspot.com"
  },
  "client": [
    {
      "client_info": {
        "mobilesdk_app_id": "1:986486622148:android:YOUR_APP_ID_HERE",
        "android_client_info": {
          "package_name": "com.pydart.stibe_one"
        }
      },
      "oauth_client": [
        {
          "client_id": "986486622148-0811nmnfmnjmnc0er554rvlqpn6dlvpl.apps.googleusercontent.com",
          "client_type": 3
        }
      ],
      "api_key": [
        {
          "current_key": "YOUR_API_KEY_HERE"
        }
      ],
      "services": {
        "appinvite_service": {
          "other_platform_oauth_client": [
            {
              "client_id": "986486622148-0811nmnfmnjmnc0er554rvlqpn6dlvpl.apps.googleusercontent.com",
              "client_type": 3
            }
          ]
        }
      }
    }
  ],
  "configuration_version": "1"
}
```

#### B. Update android/build.gradle
Add to the dependencies section:
```gradle
dependencies {
    classpath 'com.google.gms:google-services:4.3.15'
}
```

#### C. Update android/app/build.gradle
Add at the bottom of the file:
```gradle
apply plugin: 'com.google.gms.google-services'
```

Add to dependencies:
```gradle
dependencies {
    implementation 'com.google.android.gms:play-services-auth:20.7.0'
}
```

### 2. Flutter Dependencies

Add to `pubspec.yaml`:
```yaml
dependencies:
  google_sign_in: ^6.1.5
  firebase_auth: ^4.15.3
  firebase_core: ^2.24.2
```

### 3. Flutter Implementation Example

```dart
import 'package:google_sign_in/google_sign_in.dart';
import 'package:firebase_auth/firebase_auth.dart';

class GoogleAuthService {
  static const String webClientId = "986486622148-0811nmnfmnjmnc0er554rvlqpn6dlvpl.apps.googleusercontent.com";
  
  final GoogleSignIn _googleSignIn = GoogleSignIn(
    clientId: webClientId, // Use web client ID for server auth
  );

  Future<String?> signInWithGoogle() async {
    try {
      final GoogleSignInAccount? googleUser = await _googleSignIn.signIn();
      if (googleUser == null) return null;

      final GoogleSignInAuthentication googleAuth = 
          await googleUser.authentication;
      
      // Use the ID token to authenticate with your API
      return googleAuth.idToken;
    } catch (error) {
      print('Google Sign-In error: $error');
      return null;
    }
  }

  Future<void> signOut() async {
    await _googleSignIn.signOut();
  }
}
```

### 4. API Integration

To authenticate with your .NET API using the Google token:

```dart
Future<bool> loginWithGoogle() async {
  final googleToken = await GoogleAuthService().signInWithGoogle();
  if (googleToken == null) return false;

  final response = await http.post(
    Uri.parse('${ApiConfig.baseUrl}/api/auth/google-login'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'googleToken': googleToken,
      'role': 'Customer', // or 'SalonOwner'
      'acceptTerms': true,
    }),
  );

  if (response.statusCode == 200) {
    final data = jsonDecode(response.body);
    if (data['success']) {
      // Save the JWT token returned by your API
      final token = data['data']['token'];
      await storage.write(key: 'auth_token', value: token);
      return true;
    }
  }
  return false;
}
```

## Google Cloud Console Setup

### 1. Enable APIs
- Go to [Google Cloud Console](https://console.cloud.google.com/)
- Select project `stibe-booking-app`
- Enable these APIs:
  - Google+ API
  - Google Sign-In API
  - Identity and Access Management (IAM) API

### 2. Configure OAuth Consent Screen
- Set up OAuth consent screen with your app details
- Add your domain to authorized domains
- Add test users if in testing mode

### 3. Create Android OAuth Credentials
If you need separate Android credentials:
1. Go to Credentials → Create Credentials → OAuth 2.0 Client IDs
2. Select "Android" as application type
3. Enter your package name (e.g., `com.pydart.stibe_one`)
4. Add SHA-1 certificate fingerprint:
   ```bash
   # For debug builds
   keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android

   # For release builds
   keytool -list -v -keystore path/to/your/keystore.jks
   ```

## Testing

### 1. Test API Endpoints
You can test the Google OAuth endpoints using the existing debug page:
- Navigate to `/debug-google.html` in your browser
- Use the Google Sign-In button to get a token
- Test the token validation with your API

### 2. Android Testing
- Install the app on a physical device or emulator with Google Play Services
- Test the Google Sign-In flow
- Verify the token is properly sent to your API
- Check that the user is created/logged in successfully

## Security Considerations

### 1. Token Validation
- Your API validates tokens against multiple audiences (web, Android, iOS)
- Tokens are verified using Google's public keys
- Invalid tokens are rejected with appropriate error messages

### 2. User Data
- Only essential user data is stored (email, name, profile picture)
- Google ID is stored for account linking
- Passwords are auto-generated for Google users (they can't use regular login)

### 3. Role Management
- Users can register as "Customer" or "SalonOwner" via Google
- Role cannot be changed after registration without admin intervention
- Terms acceptance is required for new Google users

## Troubleshooting

### Common Issues

1. **"Invalid audience" error**
   - Ensure your client ID is correctly configured in appsettings.json
   - Verify the Google token is from the correct client

2. **"Invalid signature" error**
   - Check that your system time is correct
   - Verify Google APIs are enabled in Cloud Console

3. **"User already exists" error**
   - This happens when trying to register with Google using an email that already exists
   - User should use the login endpoint instead

### Debug Information
- Check API logs for detailed error messages
- Use the `/debug-google.html` page to test token validation
- Verify network connectivity and API availability

## Next Steps

1. **Configure each Flutter app** with the appropriate package names and SHA-1 fingerprints
2. **Test thoroughly** on both debug and release builds
3. **Update production settings** when ready to deploy
4. **Monitor API logs** for any authentication issues

Your Google OAuth setup is now ready for Android integration!

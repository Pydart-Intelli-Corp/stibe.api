// // Enhanced AuthService with Google OAuth support
// // Add this to your existing auth_service.dart in stibe_one

// import 'package:google_sign_in/google_sign_in.dart';

// class AuthService {
//   static final AuthService _instance = AuthService._internal();
//   factory AuthService() => _instance;
//   static AuthService get instance => _instance;
//   AuthService._internal();

//   final GoogleSignIn _googleSignIn = const GoogleSignIn(
//     scopes: ['email', 'profile'],
//   );

//   // Add to your existing AuthService class

//   /// Google Sign In
//   Future<LoginResponse> googleLogin({String role = "Customer"}) async {
//     try {
//       // Sign in with Google
//       final GoogleSignInAccount? account = await _googleSignIn.signIn();
      
//       if (account == null) {
//         throw Exception('Google sign in was cancelled');
//       }

//       // Get authentication details
//       final GoogleSignInAuthentication auth = await account.authentication;
      
//       if (auth.idToken == null) {
//         throw Exception('Failed to get Google ID token');
//       }

//       // Send token to your API
//       final response = await _apiService.post('/auth/google-login', data: {
//         'googleToken': auth.idToken,
//         'role': role,
//         'acceptTerms': true,
//       });

//       if (response['success'] == true && response['data'] != null) {
//         final loginData = response['data'];
        
//         // Store tokens
//         await _secureStorage.write(key: 'access_token', value: loginData['token']);
//         await _secureStorage.write(key: 'refresh_token', value: loginData['refreshToken']);
        
//         // Parse user data
//         final userData = loginData['user'];
//         final user = User.fromJson(userData);
        
//         return LoginResponse(
//           success: true,
//           message: response['message'] ?? 'Login successful',
//           user: user,
//           token: loginData['token'],
//           refreshToken: loginData['refreshToken'],
//         );
//       } else {
//         throw Exception(response['message'] ?? 'Google login failed');
//       }
//     } catch (e) {
//       throw Exception('Google login failed: ${e.toString()}');
//     }
//   }

//   /// Google Register
//   Future<bool> googleRegister({String role = "Customer"}) async {
//     try {
//       // Sign in with Google
//       final GoogleSignInAccount? account = await _googleSignIn.signIn();
      
//       if (account == null) {
//         throw Exception('Google sign in was cancelled');
//       }

//       // Get authentication details
//       final GoogleSignInAuthentication auth = await account.authentication;
      
//       if (auth.idToken == null) {
//         throw Exception('Failed to get Google ID token');
//       }

//       // Send token to your API
//       final response = await _apiService.post('/auth/google-register', data: {
//         'googleToken': auth.idToken,
//         'role': role,
//         'acceptTerms': true,
//       });

//       if (response['success'] == true) {
//         return true;
//       } else {
//         throw Exception(response['message'] ?? 'Google registration failed');
//       }
//     } catch (e) {
//       throw Exception('Google registration failed: ${e.toString()}');
//     }
//   }

//   /// Check if user is signed in with Google
//   Future<bool> isGoogleSignedIn() async {
//     return await _googleSignIn.isSignedIn();
//   }

//   /// Sign out from Google
//   Future<void> googleSignOut() async {
//     await _googleSignIn.signOut();
//   }

//   /// Get current Google user
//   GoogleSignInAccount? get currentGoogleUser => _googleSignIn.currentUser;
// }

// // Enhanced AuthProvider with Google OAuth methods
// // Add these methods to your existing auth_provider.dart

// class AuthProvider extends ChangeNotifier {
//   // ... existing code ...

//   /// Google Login
//   Future<bool> googleLogin({String role = "Customer"}) async {
//     _setLoading(true);
//     _clearError();

//     try {
//       final response = await _authService.googleLogin(role: role);
      
//       if (response.success) {
//         _currentUser = response.user;
//         _isAuthenticated = true;
        
//         notifyListeners();
//         _setLoading(false);
//         return true;
//       } else {
//         _setError(response.message);
//         _setLoading(false);
//         return false;
//       }
//     } catch (e) {
//       _setError(_getErrorMessage(e));
//       _setLoading(false);
//       return false;
//     }
//   }

//   /// Google Register
//   Future<bool> googleRegister({String role = "Customer"}) async {
//     _setLoading(true);
//     _clearError();

//     try {
//       final success = await _authService.googleRegister(role: role);
      
//       _setLoading(false);
//       return success;
//     } catch (e) {
//       _setError(_getErrorMessage(e));
//       _setLoading(false);
//       return false;
//     }
//   }

//   /// Enhanced logout that includes Google sign out
//   @override
//   Future<void> logout() async {
//     _setLoading(true);
    
//     try {
//       // Regular logout
//       await _authService.logout();
      
//       // Google logout if signed in
//       if (await _authService.isGoogleSignedIn()) {
//         await _authService.googleSignOut();
//       }
//     } catch (e) {
//       // Continue with logout even if server request fails
//       print('Logout server request failed: $e');
//     }
    
//     // Clear local state
//     _currentUser = null;
//     _isAuthenticated = false;
//     _clearError();
    
//     _setLoading(false);
    
//     // Notify listeners after clearing loading state to trigger UI update
//     notifyListeners();
//   }

//   /// Check if current user is Google user
//   bool get isGoogleUser => _currentUser?.profilePictureUrl?.contains('googleusercontent.com') == true;
// }

// // Example Flutter Google Sign-In implementation for Stibe apps
// // This file shows how to integrate Google Sign-In with your .NET API

// import 'dart:convert';
// import 'package:flutter/material.dart';
// import 'package:google_sign_in/google_sign_in.dart';
// import 'package:http/http.dart' as http;

// class GoogleAuthService {
//   // Use your web client ID for server-side token validation
//   static const String webClientId = "986486622148-0811nmnfmnjmnc0er554rvlqpn6dlvpl.apps.googleusercontent.com";
//   static const String apiBaseUrl = "https://your-api-url.com"; // Replace with your API URL
  
//   final GoogleSignIn _googleSignIn = GoogleSignIn(
//     clientId: webClientId,
//     scopes: ['email', 'profile'],
//   );

//   /// Sign in with Google and get the ID token
//   Future<String?> signInWithGoogle() async {
//     try {
//       // Trigger the authentication flow
//       final GoogleSignInAccount? googleUser = await _googleSignIn.signIn();
      
//       if (googleUser == null) {
//         // User cancelled the sign-in
//         return null;
//       }

//       // Obtain the auth details from the request
//       final GoogleSignInAuthentication googleAuth = await googleUser.authentication;

//       // Return the ID token for server validation
//       return googleAuth.idToken;
//     } catch (error) {
//       print('Google Sign-In error: $error');
//       return null;
//     }
//   }

//   /// Sign out from Google
//   Future<void> signOut() async {
//     try {
//       await _googleSignIn.signOut();
//     } catch (error) {
//       print('Google Sign-Out error: $error');
//     }
//   }

//   /// Login with Google using your .NET API
//   Future<Map<String, dynamic>?> loginWithGoogleAPI({
//     required String role, // "Customer" or "SalonOwner"
//   }) async {
//     try {
//       // Get Google ID token
//       final String? googleToken = await signInWithGoogle();
//       if (googleToken == null) return null;

//       // Send to your .NET API
//       final response = await http.post(
//         Uri.parse('$apiBaseUrl/api/auth/google-login'),
//         headers: {
//           'Content-Type': 'application/json',
//         },
//         body: jsonEncode({
//           'googleToken': googleToken,
//           'role': role,
//           'acceptTerms': true,
//         }),
//       );

//       if (response.statusCode == 200) {
//         final data = jsonDecode(response.body);
//         if (data['success']) {
//           return data['data']; // Contains token, user info, etc.
//         } else {
//           throw Exception(data['message'] ?? 'Login failed');
//         }
//       } else {
//         throw Exception('HTTP ${response.statusCode}: ${response.body}');
//       }
//     } catch (error) {
//       print('Google API login error: $error');
//       return null;
//     }
//   }

//   /// Register with Google using your .NET API
//   Future<Map<String, dynamic>?> registerWithGoogleAPI({
//     required String role, // "Customer" or "SalonOwner"
//   }) async {
//     try {
//       // Get Google ID token
//       final String? googleToken = await signInWithGoogle();
//       if (googleToken == null) return null;

//       // Send to your .NET API
//       final response = await http.post(
//         Uri.parse('$apiBaseUrl/api/auth/google-register'),
//         headers: {
//           'Content-Type': 'application/json',
//         },
//         body: jsonEncode({
//           'googleToken': googleToken,
//           'role': role,
//           'acceptTerms': true,
//         }),
//       );

//       if (response.statusCode == 200) {
//         final data = jsonDecode(response.body);
//         if (data['success']) {
//           return data['data']; // Contains user info
//         } else {
//           throw Exception(data['message'] ?? 'Registration failed');
//         }
//       } else {
//         throw Exception('HTTP ${response.statusCode}: ${response.body}');
//       }
//     } catch (error) {
//       print('Google API registration error: $error');
//       return null;
//     }
//   }

//   /// Test token validation with your API (for debugging)
//   Future<Map<String, dynamic>?> validateTokenWithAPI(String token) async {
//     try {
//       final response = await http.post(
//         Uri.parse('$apiBaseUrl/api/auth/validate-google-token'),
//         headers: {
//           'Content-Type': 'application/json',
//         },
//         body: jsonEncode({
//           'token': token,
//         }),
//       );

//       if (response.statusCode == 200) {
//         return jsonDecode(response.body);
//       } else {
//         throw Exception('HTTP ${response.statusCode}: ${response.body}');
//       }
//     } catch (error) {
//       print('Token validation error: $error');
//       return null;
//     }
//   }
// }

// // Example usage in a Flutter widget
// class GoogleSignInButton extends StatefulWidget {
//   final String userRole; // "Customer" or "SalonOwner"
//   final Function(Map<String, dynamic>) onSuccess;
//   final Function(String) onError;

//   const GoogleSignInButton({
//     Key? key,
//     required this.userRole,
//     required this.onSuccess,
//     required this.onError,
//   }) : super(key: key);

//   @override
//   _GoogleSignInButtonState createState() => _GoogleSignInButtonState();
// }

// class _GoogleSignInButtonState extends State<GoogleSignInButton> {
//   final GoogleAuthService _googleAuthService = GoogleAuthService();
//   bool _isLoading = false;

//   Future<void> _handleGoogleSignIn() async {
//     setState(() {
//       _isLoading = true;
//     });

//     try {
//       // Try login first
//       final loginResult = await _googleAuthService.loginWithGoogleAPI(
//         role: widget.userRole,
//       );

//       if (loginResult != null) {
//         widget.onSuccess(loginResult);
//       } else {
//         widget.onError('Sign-in was cancelled or failed');
//       }
//     } catch (error) {
//       // If login fails, it might be because user doesn't exist
//       // You can try registration as a fallback or show appropriate error
//       widget.onError(error.toString());
//     } finally {
//       setState(() {
//         _isLoading = false;
//       });
//     }
//   }

//   @override
//   Widget build(BuildContext context) {
//     return ElevatedButton.icon(
//       onPressed: _isLoading ? null : _handleGoogleSignIn,
//       icon: _isLoading 
//         ? const SizedBox(
//             width: 20,
//             height: 20,
//             child: CircularProgressIndicator(strokeWidth: 2),
//           )
//         : const Icon(Icons.login),
//       label: Text(_isLoading ? 'Signing in...' : 'Sign in with Google'),
//       style: ElevatedButton.styleFrom(
//         backgroundColor: Colors.white,
//         foregroundColor: Colors.black87,
//         side: const BorderSide(color: Colors.grey),
//         padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
//       ),
//     );
//   }
// }

// // Example of how to use in your login/register screen
// class LoginScreen extends StatelessWidget {
//   @override
//   Widget build(BuildContext context) {
//     return Scaffold(
//       body: Center(
//         child: Column(
//           mainAxisAlignment: MainAxisAlignment.center,
//           children: [
//             // Your regular login form here
            
//             const SizedBox(height: 20),
            
//             const Text('OR'),
            
//             const SizedBox(height: 20),
            
//             GoogleSignInButton(
//               userRole: 'Customer', // or 'SalonOwner' depending on your app
//               onSuccess: (userData) {
//                 // Handle successful login
//                 print('Login successful: $userData');
//                 // Save token, navigate to dashboard, etc.
//                 final token = userData['token'];
//                 final user = userData['user'];
                
//                 // Save token to secure storage
//                 // Navigate to main app
//                 Navigator.pushReplacementNamed(context, '/dashboard');
//               },
//               onError: (error) {
//                 // Handle login error
//                 print('Login error: $error');
//                 ScaffoldMessenger.of(context).showSnackBar(
//                   SnackBar(content: Text('Login failed: $error')),
//                 );
//               },
//             ),
//           ],
//         ),
//       ),
//     );
//   }
// }

// // Don't forget to add these dependencies to pubspec.yaml:
// /*
// dependencies:
//   flutter:
//     sdk: flutter
//   google_sign_in: ^6.1.5
//   http: ^1.1.0
  
// dev_dependencies:
//   flutter_test:
//     sdk: flutter
// */

// // And configure android/app/build.gradle:
// /*
// android {
//     ...
//     defaultConfig {
//         ...
//         multiDexEnabled true
//     }
// }

// dependencies {
//     implementation 'com.google.android.gms:play-services-auth:20.7.0'
//     implementation 'androidx.multidex:multidex:2.0.1'
// }

// apply plugin: 'com.google.gms.google-services'
// */

// // And add to android/build.gradle:
// /*
// dependencies {
//     classpath 'com.google.gms:google-services:4.3.15'
// }
// */

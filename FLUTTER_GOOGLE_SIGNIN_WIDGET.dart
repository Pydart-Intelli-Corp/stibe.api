// // Google Sign-In Button Widget for Flutter
// // Use this in your login/register screens

// import 'package:flutter/material.dart';
// import 'package:provider/provider.dart';
// import '../providers/auth_provider.dart';

// class GoogleSignInButton extends StatelessWidget {
//   final String mode; // 'login' or 'register'
//   final String role; // 'Customer' or 'SalonOwner'
//   final VoidCallback? onSuccess;
//   final Function(String)? onError;

//   const GoogleSignInButton({
//     Key? key,
//     required this.mode,
//     this.role = 'Customer',
//     this.onSuccess,
//     this.onError,
//   }) : super(key: key);

//   @override
//   Widget build(BuildContext context) {
//     return Consumer<AuthProvider>(
//       builder: (context, authProvider, child) {
//         return Container(
//           width: double.infinity,
//           height: 50,
//           child: ElevatedButton(
//             onPressed: authProvider.isLoading ? null : () => _handleGoogleSignIn(context, authProvider),
//             style: ElevatedButton.styleFrom(
//               backgroundColor: Colors.white,
//               foregroundColor: Colors.grey[700],
//               side: BorderSide(color: Colors.grey[300]!),
//               shape: RoundedRectangleBorder(
//                 borderRadius: BorderRadius.circular(8),
//               ),
//               elevation: 2,
//             ),
//             child: authProvider.isLoading
//                 ? SizedBox(
//                     height: 20,
//                     width: 20,
//                     child: CircularProgressIndicator(
//                       strokeWidth: 2,
//                       valueColor: AlwaysStoppedAnimation<Color>(Colors.grey[700]!),
//                     ),
//                   )
//                 : Row(
//                     mainAxisAlignment: MainAxisAlignment.center,
//                     children: [
//                       Image.asset(
//                         'assets/images/google_logo.png', // Add Google logo to your assets
//                         height: 24,
//                         width: 24,
//                       ),
//                       SizedBox(width: 12),
//                       Text(
//                         mode == 'login' ? 'Sign in with Google' : 'Sign up with Google',
//                         style: TextStyle(
//                           fontSize: 16,
//                           fontWeight: FontWeight.w500,
//                         ),
//                       ),
//                     ],
//                   ),
//           ),
//         );
//       },
//     );
//   }

//   Future<void> _handleGoogleSignIn(BuildContext context, AuthProvider authProvider) async {
//     try {
//       bool success = false;
      
//       if (mode == 'login') {
//         success = await authProvider.googleLogin(role: role);
//       } else {
//         success = await authProvider.googleRegister(role: role);
//         if (success) {
//           // For register, we might want to show a success message and redirect to login
//           ScaffoldMessenger.of(context).showSnackBar(
//             SnackBar(
//               content: Text('Registration successful! Please sign in.'),
//               backgroundColor: Colors.green,
//             ),
//           );
//           return;
//         }
//       }
      
//       if (success) {
//         onSuccess?.call();
//       } else if (authProvider.hasError) {
//         onError?.call(authProvider.error!);
//         ScaffoldMessenger.of(context).showSnackBar(
//           SnackBar(
//             content: Text(authProvider.error!),
//             backgroundColor: Colors.red,
//           ),
//         );
//       }
//     } catch (e) {
//       onError?.call(e.toString());
//       ScaffoldMessenger.of(context).showSnackBar(
//         SnackBar(
//           content: Text('Google sign-in failed: ${e.toString()}'),
//           backgroundColor: Colors.red,
//         ),
//       );
//     }
//   }
// }

// // Example usage in your login screen:

// class LoginScreen extends StatelessWidget {
//   @override
//   Widget build(BuildContext context) {
//     return Scaffold(
//       body: Padding(
//         padding: EdgeInsets.all(16.0),
//         child: Column(
//           mainAxisAlignment: MainAxisAlignment.center,
//           children: [
//             // Your existing login form fields
            
//             SizedBox(height: 20),
            
//             // Divider
//             Row(
//               children: [
//                 Expanded(child: Divider()),
//                 Padding(
//                   padding: EdgeInsets.symmetric(horizontal: 16),
//                   child: Text('OR'),
//                 ),
//                 Expanded(child: Divider()),
//               ],
//             ),
            
//             SizedBox(height: 20),
            
//             // Google Sign-In Button
//             GoogleSignInButton(
//               mode: 'login',
//               role: 'Customer', // or 'SalonOwner' for partner app
//               onSuccess: () {
//                 Navigator.pushReplacementNamed(context, '/dashboard');
//               },
//               onError: (error) {
//                 // Handle error if needed
//                 print('Google login error: $error');
//               },
//             ),
            
//             SizedBox(height: 16),
            
//             // Link to register
//             TextButton(
//               onPressed: () => Navigator.pushNamed(context, '/register'),
//               child: Text('Don\'t have an account? Sign up'),
//             ),
//           ],
//         ),
//       ),
//     );
//   }
// }

// // Example usage in your register screen:

// class RegisterScreen extends StatelessWidget {
//   @override
//   Widget build(BuildContext context) {
//     return Scaffold(
//       body: Padding(
//         padding: EdgeInsets.all(16.0),
//         child: Column(
//           mainAxisAlignment: MainAxisAlignment.center,
//           children: [
//             // Your existing registration form fields
            
//             SizedBox(height: 20),
            
//             // Divider
//             Row(
//               children: [
//                 Expanded(child: Divider()),
//                 Padding(
//                   padding: EdgeInsets.symmetric(horizontal: 16),
//                   child: Text('OR'),
//                 ),
//                 Expanded(child: Divider()),
//               ],
//             ),
            
//             SizedBox(height: 20),
            
//             // Google Sign-Up Button
//             GoogleSignInButton(
//               mode: 'register',
//               role: 'Customer', // or 'SalonOwner' for partner app
//               onSuccess: () {
//                 // Registration successful, redirect to login
//                 Navigator.pushReplacementNamed(context, '/login');
//               },
//               onError: (error) {
//                 // Handle error if needed
//                 print('Google registration error: $error');
//               },
//             ),
            
//             SizedBox(height: 16),
            
//             // Link to login
//             TextButton(
//               onPressed: () => Navigator.pushNamed(context, '/login'),
//               child: Text('Already have an account? Sign in'),
//             ),
//           ],
//         ),
//       ),
//     );
//   }
// }

// // Don't forget to add these dependencies to your pubspec.yaml:
// /*
// dependencies:
//   google_sign_in: ^6.1.5
//   provider: ^6.0.5

// dev_dependencies:
//   flutter_launcher_icons: ^0.13.1

// flutter:
//   assets:
//     - assets/images/google_logo.png  # Add the Google logo image
// */

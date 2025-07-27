#!/bin/bash
# Google OAuth Android Test Script

echo "=== Google OAuth Android Configuration Test ==="
echo ""

# Test API availability
echo "1. Testing API availability..."
curl -X GET "https://localhost:7000/api/auth/debug-google-auth" \
  -H "Content-Type: application/json" \
  -k

echo ""
echo ""

# Test token validation endpoint
echo "2. Testing token validation endpoint..."
echo "Note: You need to provide a real Google ID token to test this"
echo ""
echo "Example curl command:"
echo 'curl -X POST "https://localhost:7000/api/auth/validate-google-token" \'
echo '  -H "Content-Type: application/json" \'
echo '  -d "{\"token\": \"YOUR_GOOGLE_ID_TOKEN_HERE\"}" \'
echo '  -k'

echo ""
echo ""

# Test Google login endpoint
echo "3. Testing Google login endpoint..."
echo "Example curl command:"
echo 'curl -X POST "https://localhost:7000/api/auth/google-login" \'
echo '  -H "Content-Type: application/json" \'
echo '  -d "{"'
echo '    \"googleToken\": \"YOUR_GOOGLE_ID_TOKEN_HERE\",'
echo '    \"role\": \"Customer\",'
echo '    \"acceptTerms\": true'
echo '  }" \'
echo '  -k'

echo ""
echo ""

# Test Google register endpoint
echo "4. Testing Google register endpoint..."
echo "Example curl command:"
echo 'curl -X POST "https://localhost:7000/api/auth/google-register" \'
echo '  -H "Content-Type: application/json" \'
echo '  -d "{"'
echo '    \"googleToken\": \"YOUR_GOOGLE_ID_TOKEN_HERE\",'
echo '    \"role\": \"SalonOwner\",'
echo '    \"acceptTerms\": true'
echo '  }" \'
echo '  -k'

echo ""
echo "=== Test Complete ==="
echo ""
echo "To get a Google ID token for testing:"
echo "1. Use the debug page at: https://localhost:7000/debug-google.html"
echo "2. Sign in with Google and copy the ID token"
echo "3. Use that token in the curl commands above"

using stibe.api.Services.Interfaces;

using System.Security.Cryptography;
using System.Text;

namespace stibe.api.Services.Implementations.SecurityServices
{
    public class PasswordService : IPasswordService
    {
        public string HashPassword(string password)
        {
            // Using BCrypt for password hashing (you'll need to install BCrypt.Net-Next package)
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        public string GenerateSecureToken()
        {
            // Generate a cryptographically secure random token
            byte[] tokenData = new byte[32]; // 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenData);
            }

            // Convert to URL-safe Base64 string
            string token = Convert.ToBase64String(tokenData)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            return token;
        }

        public string GenerateResetToken()
        {
            // Generate a cryptographically secure random token for password resets
            byte[] tokenData = new byte[32]; // 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenData);
            }

            // Convert to URL-safe Base64 string
            string token = Convert.ToBase64String(tokenData)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            return token;
        }
    }
}
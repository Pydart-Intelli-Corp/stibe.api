using stibe.api.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace stibe.api.Services.Implementations
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

        public string GenerateResetToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
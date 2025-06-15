namespace stibe.api.Services.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        string GenerateResetToken();
        string GenerateSecureToken(); // Add this method
    }
}
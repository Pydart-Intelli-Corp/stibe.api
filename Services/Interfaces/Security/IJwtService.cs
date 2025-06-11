using stibe.api.Models.Entities;
using System.Security.Claims;

namespace stibe.api.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        int? GetUserIdFromToken(string token);
    }
}
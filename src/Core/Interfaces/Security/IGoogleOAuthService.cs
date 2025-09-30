using stibe.api.Models.DTOs.Auth;

namespace stibe.api.Services.Interfaces
{
    public interface IGoogleOAuthService
    {
        Task<GoogleUserInfoDto?> ValidateGoogleTokenAsync(string googleToken);
        Task<bool> IsGoogleTokenValidAsync(string googleToken);
    }
}

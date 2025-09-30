using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using stibe.api.Configuration;
using stibe.api.Models.DTOs.Auth;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services.Implementations.SecurityServices
{
    public class GoogleOAuthService : IGoogleOAuthService
    {
        private readonly GoogleOAuthSettings _googleSettings;
        private readonly ILogger<GoogleOAuthService> _logger;

        public GoogleOAuthService(IOptions<GoogleOAuthSettings> googleSettings, ILogger<GoogleOAuthService> logger)
        {
            _googleSettings = googleSettings.Value;
            _logger = logger;
        }

        public async Task<GoogleUserInfoDto?> ValidateGoogleTokenAsync(string googleToken)
        {
            try
            {
                if (!_googleSettings.Enabled)
                {
                    _logger.LogWarning("Google OAuth is disabled in configuration");
                    return null;
                }

                if (string.IsNullOrEmpty(_googleSettings.ClientId))
                {
                    _logger.LogError("Google OAuth ClientId is not configured");
                    return null;
                }

                // Build audience list with supported client IDs
                var audiences = new List<string> { _googleSettings.ClientId };
                
                if (!string.IsNullOrEmpty(_googleSettings.AndroidClientId))
                {
                    audiences.Add(_googleSettings.AndroidClientId);
                }

                _logger.LogInformation($"Validating Google token with audiences: {string.Join(", ", audiences)}");

                // Validate the Google token with multiple audience support
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = audiences
                });

                if (payload == null)
                {
                    _logger.LogWarning("Invalid Google token provided");
                    return null;
                }

                _logger.LogInformation($"Google token validated successfully for user: {payload.Email}");

                return new GoogleUserInfoDto
                {
                    Email = payload.Email,
                    FirstName = payload.GivenName ?? "",
                    LastName = payload.FamilyName ?? "",
                    Picture = payload.Picture ?? "",
                    EmailVerified = payload.EmailVerified,
                    GoogleId = payload.Subject
                };
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google JWT token");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google token");
                return null;
            }
        }

        public async Task<bool> IsGoogleTokenValidAsync(string googleToken)
        {
            var userInfo = await ValidateGoogleTokenAsync(googleToken);
            return userInfo != null;
        }
    }
}

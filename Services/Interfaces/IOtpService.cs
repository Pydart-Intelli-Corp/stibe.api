using stibe.api.Models.DTOs.Otp;

namespace stibe.api.Services.Interfaces
{
    public interface IOtpService
    {
        /// <summary>
        /// Generates and sends an OTP to the specified email for the given purpose
        /// </summary>
        /// <param name="email">Email address to send OTP to</param>
        /// <param name="purpose">Purpose of the OTP (e.g., EMAIL_VERIFICATION, SHOP_ACCESS)</param>
        /// <param name="ipAddress">Client IP address for security tracking</param>
        /// <param name="userAgent">Client user agent for security tracking</param>
        /// <returns>OTP response with success status and metadata</returns>
        Task<OtpResponseDto> SendOtpAsync(string email, string purpose, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Verifies an OTP code for the specified email and purpose
        /// </summary>
        /// <param name="email">Email address the OTP was sent to</param>
        /// <param name="code">6-digit OTP code to verify</param>
        /// <param name="purpose">Purpose the OTP was generated for</param>
        /// <param name="ipAddress">Client IP address for security tracking</param>
        /// <param name="userAgent">Client user agent for security tracking</param>
        /// <returns>OTP response with verification status</returns>
        Task<OtpResponseDto> VerifyOtpAsync(string email, string code, string purpose, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Gets the current OTP status for an email and purpose
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <param name="purpose">Purpose to check for</param>
        /// <returns>Current OTP status including expiry and attempts remaining</returns>
        Task<OtpStatusDto> GetOtpStatusAsync(string email, string purpose);

        /// <summary>
        /// Invalidates all pending OTPs for the specified email and purpose
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="purpose">Purpose to invalidate OTPs for</param>
        /// <returns>True if OTPs were invalidated</returns>
        Task<bool> InvalidateOtpsAsync(string email, string purpose);

        /// <summary>
        /// Cleans up expired OTP records (should be called periodically)
        /// </summary>
        /// <returns>Number of expired records cleaned up</returns>
        Task<int> CleanupExpiredOtpsAsync();

        /// <summary>
        /// Checks if an email has exceeded rate limits for OTP requests
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <param name="purpose">Purpose to check for</param>
        /// <returns>True if rate limited, false if can send</returns>
        Task<bool> IsRateLimitedAsync(string email, string purpose);
    }
}

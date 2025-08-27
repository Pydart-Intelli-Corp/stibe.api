using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Otp;
using stibe.api.Models.Entities;
using stibe.api.Services.Interfaces;
using System.Security.Cryptography;

namespace stibe.api.Services.Implementations.General
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<OtpService> _logger;

        // Configuration constants
        private const int OTP_LENGTH = 6;
        private const int OTP_EXPIRY_MINUTES = 10;
        private const int MAX_ATTEMPTS_PER_OTP = 3;
        private const int RATE_LIMIT_MINUTES = 2; // Time between OTP requests
        private const int MAX_OTPS_PER_HOUR = 30; // Maximum OTPs per email per hour (increased to effectively disable hourly limit)
        private const int CLEANUP_BATCH_SIZE = 100;

        public OtpService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<OtpService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OtpResponseDto> SendOtpAsync(string email, string purpose, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                _logger.LogInformation($"Sending OTP to {email} for purpose: {purpose}");

                // Validate input
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(purpose))
                {
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "Email and purpose are required"
                    };
                }

                // Check rate limiting
                if (await IsRateLimitedAsync(email, purpose))
                {
                    var nextAllowed = await GetNextAllowedTimeAsync(email, purpose);
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "Please wait before requesting another OTP",
                        NextAllowedAt = nextAllowed
                    };
                }

                // Invalidate any existing OTPs for this email/purpose
                await InvalidateOtpsAsync(email, purpose);

                // Generate new OTP
                var otpCode = GenerateOtpCode();
                var expiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES);

                // Create OTP entity
                var otpEntity = new OtpEntity
                {
                    Email = email.ToLower().Trim(),
                    Code = otpCode,
                    Purpose = purpose,
                    ExpiresAt = expiresAt,
                    IpAddress = ipAddress,
                    UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent
                };

                _context.OtpEntities.Add(otpEntity);
                await _context.SaveChangesAsync();

                // Send OTP via email
                var emailSent = await SendOtpEmailAsync(email, otpCode, purpose);
                if (!emailSent)
                {
                    _logger.LogError($"Failed to send OTP email to {email}");
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "Failed to send OTP. Please try again."
                    };
                }

                _logger.LogInformation($"OTP sent successfully to {email} for purpose: {purpose}");

                return new OtpResponseDto
                {
                    Success = true,
                    Message = "OTP sent successfully",
                    ExpiresAt = expiresAt,
                    AttemptsRemaining = MAX_ATTEMPTS_PER_OTP
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending OTP to {email}");
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "An error occurred while sending OTP"
                };
            }
        }

        public async Task<OtpResponseDto> VerifyOtpAsync(string email, string code, string purpose, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                _logger.LogInformation($"Verifying OTP for {email} with purpose: {purpose}");

                // Validate input
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(purpose))
                {
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "Email, code, and purpose are required"
                    };
                }

                if (code.Length != OTP_LENGTH || !code.All(char.IsDigit))
                {
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "Invalid OTP format"
                    };
                }

                // Find the most recent valid OTP
                var otpEntity = await _context.OtpEntities
                    .Where(o => o.Email == email.ToLower().Trim() 
                            && o.Purpose == purpose 
                            && !o.IsUsed 
                            && o.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpEntity == null)
                {
                    _logger.LogWarning($"No valid OTP found for {email} with purpose: {purpose}");
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "No valid OTP found. Please request a new one."
                    };
                }

                // Check attempt limits
                if (otpEntity.AttemptCount >= MAX_ATTEMPTS_PER_OTP)
                {
                    _logger.LogWarning($"Max attempts exceeded for OTP {otpEntity.Id}");
                    otpEntity.IsUsed = true; // Mark as used to prevent further attempts
                    await _context.SaveChangesAsync();

                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "Maximum verification attempts exceeded. Please request a new OTP."
                    };
                }

                // Increment attempt count
                otpEntity.AttemptCount++;
                otpEntity.LastAttemptAt = DateTime.UtcNow;

                // Verify the code
                if (otpEntity.Code != code)
                {
                    await _context.SaveChangesAsync();
                    
                    var attemptsRemaining = MAX_ATTEMPTS_PER_OTP - otpEntity.AttemptCount;
                    _logger.LogWarning($"Invalid OTP code for {email}. Attempts remaining: {attemptsRemaining}");
                    
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = $"Invalid OTP code. {attemptsRemaining} attempts remaining.",
                        AttemptsRemaining = attemptsRemaining
                    };
                }

                // OTP is valid - mark as used
                otpEntity.IsUsed = true;
                otpEntity.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"OTP verified successfully for {email} with purpose: {purpose}");

                return new OtpResponseDto
                {
                    Success = true,
                    Message = "OTP verified successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying OTP for {email}");
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "An error occurred while verifying OTP"
                };
            }
        }

        public async Task<OtpStatusDto> GetOtpStatusAsync(string email, string purpose)
        {
            try
            {
                var otpEntity = await _context.OtpEntities
                    .Where(o => o.Email == email.ToLower().Trim() 
                            && o.Purpose == purpose 
                            && !o.IsUsed 
                            && o.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpEntity == null)
                {
                    return new OtpStatusDto
                    {
                        HasPendingOtp = false,
                        CanRequestNew = !await IsRateLimitedAsync(email, purpose)
                    };
                }

                var attemptsRemaining = Math.Max(0, MAX_ATTEMPTS_PER_OTP - otpEntity.AttemptCount);
                var nextAllowed = await GetNextAllowedTimeAsync(email, purpose);

                return new OtpStatusDto
                {
                    HasPendingOtp = true,
                    Purpose = purpose,
                    ExpiresAt = otpEntity.ExpiresAt,
                    AttemptsRemaining = attemptsRemaining,
                    NextAllowedAt = nextAllowed,
                    CanRequestNew = nextAllowed <= DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting OTP status for {email}");
                return new OtpStatusDto { HasPendingOtp = false };
            }
        }

        public async Task<bool> InvalidateOtpsAsync(string email, string purpose)
        {
            try
            {
                var otpsToInvalidate = await _context.OtpEntities
                    .Where(o => o.Email == email.ToLower().Trim() 
                            && o.Purpose == purpose 
                            && !o.IsUsed)
                    .ToListAsync();

                foreach (var otp in otpsToInvalidate)
                {
                    otp.IsUsed = true;
                    otp.UsedAt = DateTime.UtcNow;
                }

                if (otpsToInvalidate.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Invalidated {otpsToInvalidate.Count} OTPs for {email} with purpose: {purpose}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invalidating OTPs for {email}");
                return false;
            }
        }

        public async Task<int> CleanupExpiredOtpsAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-7); // Clean up OTPs older than 7 days
                
                var expiredOtps = await _context.OtpEntities
                    .Where(o => o.CreatedAt < cutoffDate || (o.ExpiresAt < DateTime.UtcNow && o.IsUsed))
                    .Take(CLEANUP_BATCH_SIZE)
                    .ToListAsync();

                if (expiredOtps.Any())
                {
                    _context.OtpEntities.RemoveRange(expiredOtps);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Cleaned up {expiredOtps.Count} expired OTP records");
                    return expiredOtps.Count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired OTPs");
                return 0;
            }
        }

        public async Task<bool> IsRateLimitedAsync(string email, string purpose)
        {
            try
            {
                var now = DateTime.UtcNow;
                var hourAgo = now.AddHours(-1);
                var rateLimitWindow = now.AddMinutes(-RATE_LIMIT_MINUTES);

                _logger.LogInformation($"Rate limit check for {email}, purpose: {purpose}");
                _logger.LogInformation($"Current time: {now:yyyy-MM-dd HH:mm:ss.fff} UTC");
                _logger.LogInformation($"Rate limit window (must be after): {rateLimitWindow:yyyy-MM-dd HH:mm:ss.fff} UTC");

                // Check if user has exceeded hourly limit
                var hourlyCount = await _context.OtpEntities
                    .CountAsync(o => o.Email == email.ToLower().Trim() 
                                && o.Purpose == purpose 
                                && o.CreatedAt >= hourAgo);

                _logger.LogInformation($"Hourly OTP count: {hourlyCount}/{MAX_OTPS_PER_HOUR}");

                if (hourlyCount >= MAX_OTPS_PER_HOUR)
                {
                    _logger.LogWarning($"Hourly rate limit exceeded for {email} with purpose: {purpose}");
                    return true;
                }

                // Check if user has requested an OTP too recently
                var recentOtp = await _context.OtpEntities
                    .Where(o => o.Email == email.ToLower().Trim() 
                              && o.Purpose == purpose 
                              && o.CreatedAt >= rateLimitWindow)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (recentOtp != null)
                {
                    _logger.LogWarning($"Rate limit active for {email} with purpose: {purpose}");
                    _logger.LogWarning($"Most recent OTP created at: {recentOtp.CreatedAt:yyyy-MM-dd HH:mm:ss.fff} UTC");
                    _logger.LogWarning($"Next allowed time: {recentOtp.CreatedAt.AddMinutes(RATE_LIMIT_MINUTES):yyyy-MM-dd HH:mm:ss.fff} UTC");
                    return true;
                }

                _logger.LogInformation($"Rate limit check passed for {email}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking rate limit for {email}");
                return true; // Fail safe - assume rate limited on error
            }
        }

        private string GenerateOtpCode()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var random = BitConverter.ToUInt32(bytes, 0);
                var code = (random % 1000000).ToString("D6");
                return code;
            }
        }

        private async Task<bool> SendOtpEmailAsync(string email, string otpCode, string purpose)
        {
            try
            {
                var subject = GetEmailSubject(purpose);
                var body = GetEmailBody(otpCode, purpose);

                return await _emailService.SendEmailAsync(email, subject, body, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending OTP email to {email}");
                return false;
            }
        }

        private string GetEmailSubject(string purpose)
        {
            return purpose switch
            {
                OtpEntity.PURPOSE_EMAIL_VERIFICATION => "Email Verification Code - Stibe",
                OtpEntity.PURPOSE_SALON_ACCESS => "Salon Access Code - Stibe",
                OtpEntity.PURPOSE_SALON_STATUS_CHANGE => "Salon Status Change Verification - Stibe",
                OtpEntity.PURPOSE_SALON_DEFAULT_CHANGE => "Default Salon Change Verification - Stibe",
                OtpEntity.PURPOSE_SALON_DELETE => "Salon Deletion Verification - Stibe",
                OtpEntity.PURPOSE_PASSWORD_RESET => "Password Reset Code - Stibe",
                OtpEntity.PURPOSE_PHONE_VERIFICATION => "Phone Verification Code - Stibe",
                OtpEntity.PURPOSE_TWO_FACTOR_AUTH => "Two-Factor Authentication Code - Stibe",
                _ => "Verification Code - Stibe"
            };
        }

        private string GetEmailBody(string otpCode, string purpose)
        {
            var purposeText = purpose switch
            {
                OtpEntity.PURPOSE_EMAIL_VERIFICATION => "verify your email address",
                OtpEntity.PURPOSE_SALON_ACCESS => "access salon editing features",
                OtpEntity.PURPOSE_SALON_STATUS_CHANGE => "change your salon's active status",
                OtpEntity.PURPOSE_SALON_DEFAULT_CHANGE => "change your default salon setting",
                OtpEntity.PURPOSE_SALON_DELETE => "permanently delete your salon",
                OtpEntity.PURPOSE_PASSWORD_RESET => "reset your password",
                OtpEntity.PURPOSE_PHONE_VERIFICATION => "verify your phone number",
                OtpEntity.PURPOSE_TWO_FACTOR_AUTH => "complete two-factor authentication",
                _ => "complete verification"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Verification Code</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .email-container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4A86E8; padding: 30px; text-align: center; color: white; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 30px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px; background-color: #f9f9f9; }}
        .otp-code {{ 
            font-size: 36px; 
            font-weight: bold; 
            color: #4A86E8; 
            text-align: center; 
            letter-spacing: 8px; 
            margin: 20px 0; 
            padding: 20px; 
            background-color: white; 
            border: 2px dashed #4A86E8; 
            border-radius: 8px;
        }}
        .warning {{ 
            background-color: #fff3cd; 
            border: 1px solid #ffeaa7; 
            color: #856404; 
            padding: 15px; 
            border-radius: 4px; 
            margin: 20px 0; 
        }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #777; text-align: center; }}
        .highlight {{ color: #4A86E8; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1>🔐 Verification Code</h1>
        </div>
        <div class='content'>
            <p>Hello,</p>
            
            <p>You requested a verification code to <span class='highlight'>{purposeText}</span>. Please use the following 6-digit code:</p>
            
            <div class='otp-code'>{otpCode}</div>
            
            <div class='warning'>
                <strong>⚠️ Important Security Information:</strong><br>
                • This code will expire in <strong>10 minutes</strong><br>
                • You have <strong>3 attempts</strong> to enter the correct code<br>
                • Never share this code with anyone<br>
                • If you didn't request this code, please ignore this email
            </div>
            
            <p>For your security, this code can only be used once and will expire automatically.</p>
            
            <p>If you're having trouble or didn't request this code, please contact our support team.</p>
            
            <p>Best regards,<br>
            <strong>The Stibe Team</strong></p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} Stibe Booking. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private async Task<DateTime?> GetNextAllowedTimeAsync(string email, string purpose)
        {
            try
            {
                var now = DateTime.UtcNow;
                var hourAgo = now.AddHours(-1);
                var rateLimitWindow = now.AddMinutes(-RATE_LIMIT_MINUTES);

                _logger.LogInformation($"Getting next allowed time for {email}, purpose: {purpose}");

                // Check hourly rate limit first
                var hourlyOtps = await _context.OtpEntities
                    .Where(o => o.Email == email.ToLower().Trim() 
                              && o.Purpose == purpose 
                              && o.CreatedAt >= hourAgo)
                    .OrderBy(o => o.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"Found {hourlyOtps.Count} OTPs in the last hour");

                if (hourlyOtps.Count >= MAX_OTPS_PER_HOUR)
                {
                    // User has hit hourly limit, return when the oldest OTP in the window expires
                    var oldestOtpInWindow = hourlyOtps.First();
                    var hourlyResetTime = oldestOtpInWindow.CreatedAt.AddHours(1);
                    
                    _logger.LogInformation($"Hourly rate limit exceeded. Reset time: {hourlyResetTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
                    return hourlyResetTime;
                }

                // Check 2-minute rate limit
                var lastOtp = await _context.OtpEntities
                    .Where(o => o.Email == email.ToLower().Trim() && o.Purpose == purpose)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastOtp == null)
                {
                    _logger.LogInformation("No previous OTP found, allowing immediately");
                    return now; // No previous OTP, allow immediately
                }

                var nextAllowedTime = lastOtp.CreatedAt.AddMinutes(RATE_LIMIT_MINUTES);
                
                _logger.LogInformation($"Last OTP at: {lastOtp.CreatedAt:yyyy-MM-dd HH:mm:ss.fff} UTC");
                _logger.LogInformation($"Next allowed time: {nextAllowedTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
                
                // If the rate limit period has already passed, allow immediately
                if (nextAllowedTime <= now)
                {
                    _logger.LogInformation("Rate limit period has passed, allowing immediately");
                    return now;
                }

                return nextAllowedTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting next allowed time for {email}");
                return DateTime.UtcNow.AddMinutes(RATE_LIMIT_MINUTES);
            }
        }
    }
}

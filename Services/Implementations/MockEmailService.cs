using Microsoft.Extensions.Options;
using stibe.api.Configuration;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services.Implementations
{
    public class MockEmailService : IEmailService
    {
        private readonly FeatureFlags _featureFlags;
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(IOptions<FeatureFlags> featureFlags, ILogger<MockEmailService> logger)
        {
            _featureFlags = featureFlags.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            if (_featureFlags.UseRealEmailService)
            {
                // TODO: Implement real email service (SMTP, SendGrid, etc.)
                throw new NotImplementedException("Real email service not implemented yet");
            }

            // Mock implementation - just log the email
            _logger.LogInformation("=== MOCK EMAIL SERVICE ===");
            _logger.LogInformation($"To: {to}");
            _logger.LogInformation($"Subject: {subject}");
            _logger.LogInformation($"Body: {body}");
            _logger.LogInformation($"Is HTML: {isHtml}");
            _logger.LogInformation("=== END MOCK EMAIL ===");

            // Simulate email sending delay
            await Task.Delay(100);

            return true;
        }

        public async Task<bool> SendVerificationEmailAsync(string to, string verificationLink)
        {
            var subject = "Verify Your Email - Stibe Booking";
            var body = $@"
                <h2>Welcome to Stibe Booking!</h2>
                <p>Please click the link below to verify your email address:</p>
                <a href=""{verificationLink}"">Verify Email</a>
                <p>If you didn't create an account, please ignore this email.</p>";

            return await SendEmailAsync(to, subject, body);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string to, string resetLink)
        {
            var subject = "Reset Your Password - Stibe Booking";
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>Click the link below to reset your password:</p>
                <a href=""{resetLink}"">Reset Password</a>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>";

            return await SendEmailAsync(to, subject, body);
        }

        public async Task<bool> SendBookingConfirmationEmailAsync(string to, string bookingDetails)
        {
            var subject = "Booking Confirmation - Stibe Booking";
            var body = $@"
                <h2>Booking Confirmed!</h2>
                <p>Your booking has been confirmed. Here are the details:</p>
                <div>{bookingDetails}</div>
                <p>Thank you for choosing Stibe Booking!</p>";

            return await SendEmailAsync(to, subject, body);
        }
    }
}
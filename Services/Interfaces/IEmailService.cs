namespace stibe.api.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendVerificationEmailAsync(string to, string verificationLink);
        Task<bool> SendPasswordResetEmailAsync(string to, string resetLink);
        Task<bool> SendBookingConfirmationEmailAsync(string to, string bookingDetails);
    }
}
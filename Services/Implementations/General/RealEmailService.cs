using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using stibe.api.Configuration;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services.Implementations.General
{
    public class RealEmailService : IEmailService
    {
        private readonly EmailConfiguration _emailConfig;
        private readonly ILogger<RealEmailService> _logger;

        public RealEmailService(IOptions<EmailConfiguration> emailConfig, ILogger<RealEmailService> logger)
        {
            _emailConfig = emailConfig.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailConfig.SenderName, _emailConfig.SenderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(_emailConfig.Host, _emailConfig.Port,
     _emailConfig.EnableSSL ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

                if (!string.IsNullOrEmpty(_emailConfig.Username))
                    await client.AuthenticateAsync(_emailConfig.Username, _emailConfig.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {to}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                return false;
            }
        }

        public async Task<bool> SendVerificationEmailAsync(string to, string verificationLink)
        {
            var subject = "Verify Your Email - Stibe Booking";
            var body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Email Verification</title>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .email-container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background-color: #4A86E8; padding: 20px; text-align: center; color: white; }}
                .content {{ padding: 20px; border: 1px solid #ddd; border-top: none; }}
                .button {{ display: inline-block; background-color: #4CAF50; color: white; padding: 12px 24px; 
                           text-decoration: none; border-radius: 4px; font-weight: bold; margin: 20px 0; }}
                .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
                .note {{ font-size: 13px; margin-top: 15px; color: #666; }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='header'>
                    <h2>Welcome to Stibe Booking!</h2>
                </div>
                <div class='content'>
                    <p>Thank you for registering with Stibe Booking. To complete your registration, please verify your email address by clicking the button below:</p>
                    
                    <div style='text-align: center;'>
                        <a href='{verificationLink}' class='button'>VERIFY MY EMAIL</a>
                    </div>
                    
                    <p class='note'>If you're having trouble clicking the button, copy and paste the following URL into your browser:</p>
                    <p><a href='{verificationLink}'>{verificationLink}</a></p>
                    
                    <p>This verification link will expire in 24 hours.</p>
                    
                    <p>If you didn't create an account with Stibe Booking, please ignore this email.</p>
                </div>
                <div class='footer'>
                    <p>&copy; {DateTime.Now.Year} Stibe Booking. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";

            return await SendEmailAsync(to, subject, body);
        }


        public async Task<bool> SendPasswordResetEmailAsync(string to, string resetLink)
        {
            var subject = "Reset Your Password - Stibe Booking";
            var body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Password Reset</title>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .email-container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background-color: #4A86E8; padding: 20px; text-align: center; color: white; }}
                .content {{ padding: 20px; border: 1px solid #ddd; border-top: none; }}
                .button {{ display: inline-block; background-color: #4CAF50; color: white; padding: 12px 24px; 
                           text-decoration: none; border-radius: 4px; font-weight: bold; margin: 20px 0; }}
                .button:hover {{ background-color: #45a049; }}
                .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
                .note {{ font-size: 13px; margin-top: 15px; color: #666; }}
                .warning {{ color: #e74c3c; font-weight: bold; }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='header'>
                    <h2>Password Reset Request</h2>
                </div>
                <div class='content'>
                    <p>You recently requested to reset your password for your Stibe Booking account. Click the button below to reset it:</p>
                    
                    <div style='text-align: center;'>
                        <a href='{resetLink}' class='button'>RESET MY PASSWORD</a>
                    </div>
                    
                    <p class='note'>If you're having trouble clicking the button, copy and paste the following URL into your browser:</p>
                    <p><a href='{resetLink}'>{resetLink}</a></p>
                    
                    <p class='warning'>This password reset link will expire in 1 hour for security reasons.</p>
                    
                    <p>If you didn't request a password reset, please ignore this email or contact support if you have concerns.</p>
                </div>
                <div class='footer'>
                    <p>&copy; {DateTime.Now.Year} Stibe Booking. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";

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

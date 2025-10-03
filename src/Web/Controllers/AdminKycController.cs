using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Services.Interfaces;
using stibe.api.Models.Entities;
using stibe.api.Models.Entities.PartnersEntity;

namespace stibe.api.Controllers
{
    [Route("admin/kyc")]
    public class AdminKycController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminKycController> _logger;

        public AdminKycController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AdminKycController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet("approve/{userId}")]
        public async Task<IActionResult> ApproveKyc(int userId, string? token)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var kycRecords = await _context.KycVerifications
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                // For now, automatically approve the first pending KYC record
                var pendingKyc = kycRecords.FirstOrDefault(k => k.Status == "Pending");
                if (pendingKyc != null)
                {
                    pendingKyc.Status = "Approved";
                    pendingKyc.VerifiedAt = DateTime.UtcNow;
                    pendingKyc.AdminNotes = "Approved via email link";

                    // Update user KYC status
                    user.KycStatus = "Verified";
                    user.IsKycVerified = true;
                    user.KycVerifiedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    // Send approval email to user
                    await SendKycApprovalEmailToUser(user, "Your KYC has been approved by admin");

                    return Ok(new 
                    { 
                        success = true, 
                        message = "KYC approved successfully", 
                        user = new { user.Id, user.FirstName, user.LastName, user.Email, user.KycStatus },
                        kycRecord = new { pendingKyc.Id, pendingKyc.Status, pendingKyc.VerifiedAt }
                    });
                }

                return BadRequest(new { success = false, message = "No pending KYC record found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving KYC for user {UserId}", userId);
                return BadRequest("Error loading KYC data");
            }
        }

        [HttpPost("approve/{userId}")]
        public async Task<IActionResult> ConfirmApproveKyc(int userId, string adminNotes)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Update user status
                user.IsKycVerified = true;
                user.KycStatus = "Verified";
                user.KycVerifiedAt = DateTime.UtcNow;
                user.KycRejectionReason = null;

                // Update KYC records
                var kycRecords = await _context.KycVerifications
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                foreach (var record in kycRecords)
                {
                    record.Status = "Approved";
                    record.VerifiedAt = DateTime.UtcNow;
                    record.AdminNotes = adminNotes;
                }

                await _context.SaveChangesAsync();

                // Send approval email
                await SendKycApprovalEmailToUser(user, adminNotes);

                return View("KycActionComplete", new { 
                    Action = "Approved", 
                    UserName = $"{user.FirstName} {user.LastName}",
                    UserId = userId 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving KYC for user {UserId}", userId);
                return BadRequest("Error processing KYC approval");
            }
        }

        [HttpGet("reject/{userId}")]
        public async Task<IActionResult> RejectKyc(int userId, string? token)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var kycRecords = await _context.KycVerifications
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                // For now, automatically reject the first pending KYC record
                var pendingKyc = kycRecords.FirstOrDefault(k => k.Status == "Pending");
                if (pendingKyc != null)
                {
                    pendingKyc.Status = "Rejected";
                    pendingKyc.VerifiedAt = DateTime.UtcNow;
                    pendingKyc.RejectionReason = "Rejected via email link - manual review required";
                    pendingKyc.AdminNotes = "Rejected via email link";

                    // Update user KYC status
                    user.KycStatus = "Rejected";
                    user.IsKycVerified = false;
                    user.KycRejectionReason = "Documents need manual review";

                    await _context.SaveChangesAsync();

                    // Send rejection email to user
                    await SendKycRejectionEmailToUser(user, "Your KYC requires additional documentation", "Rejected via email link");

                    return Ok(new 
                    { 
                        success = true, 
                        message = "KYC rejected successfully", 
                        user = new { user.Id, user.FirstName, user.LastName, user.Email, user.KycStatus },
                        kycRecord = new { pendingKyc.Id, pendingKyc.Status, pendingKyc.RejectionReason }
                    });
                }

                return BadRequest(new { success = false, message = "No pending KYC record found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting KYC for user {UserId}", userId);
                return BadRequest(new { success = false, message = "Error processing KYC rejection" });
            }
        }

        [HttpPost("reject/{userId}")]
        public async Task<IActionResult> ConfirmRejectKyc(int userId, string rejectionReason, string adminNotes)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Update user status
                user.IsKycVerified = false;
                user.KycStatus = "Rejected";
                user.KycRejectionReason = rejectionReason;

                // Update KYC records
                var kycRecords = await _context.KycVerifications
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                foreach (var record in kycRecords)
                {
                    record.Status = "Rejected";
                    record.RejectionReason = rejectionReason;
                    record.AdminNotes = adminNotes;
                }

                await _context.SaveChangesAsync();

                // Send rejection email
                await SendKycRejectionEmailToUser(user, rejectionReason, adminNotes);

                return View("KycActionComplete", new { 
                    Action = "Rejected", 
                    UserName = $"{user.FirstName} {user.LastName}",
                    UserId = userId 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting KYC for user {UserId}", userId);
                return BadRequest("Error processing KYC rejection");
            }
        }

        // Email helper methods (copied from KycController)
        private async Task SendKycApprovalEmailToUser(User user, string? adminNotes)
        {
            try
            {
                var subject = "🎉 KYC Approved - Welcome to Stibe!";
                
                var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>KYC Approved</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; }}
                        .email-container {{ max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; overflow: hidden; }}
                        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 30px; text-align: center; color: white; }}
                        .content {{ padding: 30px; background-color: #f9f9f9; }}
                        .success-badge {{ background-color: #28a745; color: white; padding: 10px 20px; border-radius: 25px; display: inline-block; margin: 15px 0; font-weight: bold; }}
                        .info-box {{ background-color: white; padding: 20px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #28a745; }}
                        .footer {{ padding: 20px; text-align: center; background-color: #f1f1f1; font-size: 12px; color: #666; }}
                        .next-steps {{ background-color: #e3f2fd; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <div class='header'>
                            <h1>🎉 Congratulations!</h1>
                            <div class='success-badge'>✅ KYC APPROVED</div>
                            <p>Your identity verification has been successfully completed</p>
                        </div>
                        
                        <div class='content'>
                            <p>Dear {user.FirstName} {user.LastName},</p>
                            
                            <p>Great news! Your KYC (Know Your Customer) verification has been <strong>approved</strong> by our team.</p>
                            
                            <div class='info-box'>
                                <h3>✅ Verification Details</h3>
                                <p><strong>Status:</strong> Approved</p>
                                <p><strong>Verified Date:</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</p>
                                <p><strong>Account Type:</strong> Verified Business Account</p>
                            </div>
                            
                            {(!string.IsNullOrEmpty(adminNotes) ? $@"
                            <div class='info-box'>
                                <h3>📝 Admin Notes</h3>
                                <p>{adminNotes}</p>
                            </div>" : "")}
                            
                            <div class='next-steps'>
                                <h3>🚀 What's Next?</h3>
                                <ul>
                                    <li>✅ Your account is now fully verified</li>
                                    <li>✅ You can now access all business features</li>
                                    <li>✅ Start listing your services on Stibe platform</li>
                                    <li>✅ Accept bookings from customers</li>
                                    <li>✅ Access advanced business analytics</li>
                                </ul>
                            </div>
                            
                            <p>Thank you for completing the verification process. We're excited to have you as a verified partner on Stibe!</p>
                            
                            <p>Best regards,<br>
                            <strong>Team Stibe</strong></p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated email. Please do not reply to this message.</p>
                            <p>&copy; {DateTime.Now.Year} Stibe. All rights reserved.</p>
                            <p>Contact us: info.pydart@gmail.com</p>
                        </div>
                    </div>
                </body>
                </html>";

                await _emailService.SendEmailAsync(user.Email, subject, body, true);
                _logger.LogInformation("KYC approval email sent to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send KYC approval email to user {UserId}", user.Id);
            }
        }

        private async Task SendKycRejectionEmailToUser(User user, string rejectionReason, string? adminNotes)
        {
            try
            {
                var subject = "❌ KYC Verification Update - Action Required";
                
                var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>KYC Verification Update</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; }}
                        .email-container {{ max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; overflow: hidden; }}
                        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); padding: 30px; text-align: center; color: white; }}
                        .content {{ padding: 30px; background-color: #f9f9f9; }}
                        .warning-badge {{ background-color: #dc3545; color: white; padding: 10px 20px; border-radius: 25px; display: inline-block; margin: 15px 0; font-weight: bold; }}
                        .info-box {{ background-color: white; padding: 20px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #dc3545; }}
                        .footer {{ padding: 20px; text-align: center; background-color: #f1f1f1; font-size: 12px; color: #666; }}
                        .action-required {{ background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #ffeaa7; }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <div class='header'>
                            <h1>📋 KYC Verification Update</h1>
                            <div class='warning-badge'>⚠️ REVIEW REQUIRED</div>
                            <p>Your KYC submission requires attention</p>
                        </div>
                        
                        <div class='content'>
                            <p>Dear {user.FirstName} {user.LastName},</p>
                            
                            <p>Thank you for submitting your KYC documents. After careful review, we need you to address some issues with your submission.</p>
                            
                            <div class='info-box'>
                                <h3>📋 Review Details</h3>
                                <p><strong>Status:</strong> Needs Revision</p>
                                <p><strong>Review Date:</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</p>
                                <p><strong>Reason:</strong> {rejectionReason}</p>
                            </div>
                            
                            {(!string.IsNullOrEmpty(adminNotes) ? $@"
                            <div class='info-box'>
                                <h3>📝 Detailed Feedback</h3>
                                <p>{adminNotes}</p>
                            </div>" : "")}
                            
                            <p>If you have any questions about this feedback, please contact our support team.</p>
                            
                            <p>Best regards,<br>
                            <strong>Team Stibe</strong></p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated email. Please do not reply to this message.</p>
                            <p>&copy; {DateTime.Now.Year} Stibe. All rights reserved.</p>
                            <p>Contact us: info.pydart@gmail.com</p>
                        </div>
                    </div>
                </body>
                </html>";

                await _emailService.SendEmailAsync(user.Email, subject, body, true);
                _logger.LogInformation("KYC rejection email sent to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send KYC rejection email to user {UserId}", user.Id);
            }
        }
    }

    // View model for admin KYC pages
    public class AdminKycViewModel
    {
        public User User { get; set; } = null!;
        public List<KycVerification> KycRecords { get; set; } = new();
        public string Action { get; set; } = "";
    }
}
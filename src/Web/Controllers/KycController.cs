using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.Auth;
using stibe.api.Models.Entities;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KycController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly ILogger<KycController> _logger;
        private readonly IEmailService _emailService;

        public KycController(
            ApplicationDbContext context,
            IFileService fileService,
            ILogger<KycController> logger,
            IEmailService emailService)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost("verify/aadhaar")]
        public async Task<ActionResult<ApiResponse<KycVerificationResponseDto>>> VerifyAadhaar(
            [FromBody] AadhaarVerificationDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<KycVerificationResponseDto>.ErrorResponse("Unauthorized"));
                }

                // Validate Aadhaar number format
                if (!IsValidAadhaarNumber(request.AadhaarNumber))
                {
                    return BadRequest(ApiResponse<KycVerificationResponseDto>.ErrorResponse("Invalid Aadhaar number format"));
                }

                // Check if Aadhaar is already used by another user
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.AadhaarNumber == request.AadhaarNumber && u.Id != userId);
                
                if (existingUser != null)
                {
                    return BadRequest(ApiResponse<KycVerificationResponseDto>.ErrorResponse("Aadhaar number already in use"));
                }

                // MVP: Skip external API verification, return success for valid format
                var verificationResult = new KycVerificationResponseDto
                {
                    Success = true,
                    Message = "Aadhaar number validated successfully (MVP mode)",
                    ConfidenceScore = 85.0f,
                    ExtractedData = new KycExtractedDataDto
                    {
                        DocumentNumber = request.AadhaarNumber,
                        ConfidenceScore = 85.0f,
                        AdditionalFields = new Dictionary<string, object>
                        {
                            ["Status"] = "Validated",
                            ["DocumentType"] = "Aadhaar"
                        }
                    }
                };

                // Store verification result
                await StoreKycVerificationResult(userId.Value, "aadhaar", verificationResult);

                return Ok(ApiResponse<KycVerificationResponseDto>.SuccessResponse(
                    verificationResult, 
                    "Aadhaar validated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Aadhaar for user");
                return StatusCode(500, ApiResponse<KycVerificationResponseDto>.ErrorResponse(
                    "An error occurred while verifying Aadhaar"));
            }
        }

        [HttpPost("verify/pan")]
        public async Task<ActionResult<ApiResponse<KycVerificationResponseDto>>> VerifyPan(
            [FromBody] PanVerificationDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<KycVerificationResponseDto>.ErrorResponse("Unauthorized"));
                }

                // Validate PAN number format
                if (!IsValidPanNumber(request.PanNumber))
                {
                    return BadRequest(ApiResponse<KycVerificationResponseDto>.ErrorResponse("Invalid PAN number format"));
                }

                // Check if PAN is already used by another user
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.PanNumber == request.PanNumber && u.Id != userId);
                
                if (existingUser != null)
                {
                    return BadRequest(ApiResponse<KycVerificationResponseDto>.ErrorResponse("PAN number already in use"));
                }

                // MVP: Skip external API verification, return success for valid format
                var verificationResult = new KycVerificationResponseDto
                {
                    Success = true,
                    Message = "PAN number validated successfully (MVP mode)",
                    ConfidenceScore = 85.0f,
                    ExtractedData = new KycExtractedDataDto
                    {
                        DocumentNumber = request.PanNumber,
                        Name = request.FullName,
                        DateOfBirth = request.DateOfBirth,
                        ConfidenceScore = 85.0f,
                        AdditionalFields = new Dictionary<string, object>
                        {
                            ["Status"] = "Validated",
                            ["DocumentType"] = "PAN"
                        }
                    }
                };

                // Store verification result
                await StoreKycVerificationResult(userId.Value, "pan", verificationResult);

                return Ok(ApiResponse<KycVerificationResponseDto>.SuccessResponse(
                    verificationResult, 
                    "PAN validated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying PAN for user");
                return StatusCode(500, ApiResponse<KycVerificationResponseDto>.ErrorResponse(
                    "An error occurred while verifying PAN"));
            }
        }

        [HttpPost("ocr/extract")]
        public async Task<ActionResult<ApiResponse<KycExtractedDataDto>>> ExtractDocumentData(
            [FromBody] DocumentOcrDto request)
        {
            try
            {
                // MVP: Return mock extracted data instead of external API call
                var extractedData = new KycExtractedDataDto
                {
                    ConfidenceScore = 85.0f,
                    AdditionalFields = new Dictionary<string, object>
                    {
                        ["document_type"] = request.DocumentType,
                        ["status"] = "Extracted (MVP mode)",
                        ["confidence"] = "85.0"
                    }
                };

                return Ok(ApiResponse<KycExtractedDataDto>.SuccessResponse(
                    extractedData, 
                    "Document data extracted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting document data");
                return StatusCode(500, ApiResponse<KycExtractedDataDto>.ErrorResponse(
                    "An error occurred while extracting document data"));
            }
        }

        [HttpPost("face-verification")]
        public async Task<ActionResult<ApiResponse<FaceVerificationDto>>> VerifyFace(
            [FromBody] FaceVerificationRequestDto request)
        {
            try
            {
                // MVP: Return mock face verification result instead of external API call
                var result = new FaceVerificationDto
                {
                    IsMatch = true,
                    ConfidenceScore = 88.5f,
                    Message = "Face verification successful (MVP mode)"
                };

                return Ok(ApiResponse<FaceVerificationDto>.SuccessResponse(
                    result, 
                    "Face verification successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during face verification");
                return StatusCode(500, ApiResponse<FaceVerificationDto>.ErrorResponse(
                    "An error occurred during face verification"));
            }
        }

        [HttpPost("submit")]
        public async Task<ActionResult<ApiResponse<UserDto>>> SubmitCompleteKyc(
            [FromBody] CompleteKycSubmissionDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResponse("Unauthorized"));
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));
                }

                // Validate all submitted data
                var validationErrors = await ValidateKycSubmission(request, userId.Value);
                if (validationErrors.Any())
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse(
                        "Validation failed: " + string.Join(", ", validationErrors)));
                }

                // Upload document images
                var frontImageUrl = await _fileService.UploadFileAsync(
                    ConvertBase64ToIFormFile(request.FrontDocumentImageBase64, "front.jpg"),
                    "kyc/documents");

                string? backImageUrl = null;
                if (!string.IsNullOrEmpty(request.BackDocumentImageBase64))
                {
                    backImageUrl = await _fileService.UploadFileAsync(
                        ConvertBase64ToIFormFile(request.BackDocumentImageBase64, "back.jpg"),
                        "kyc/documents");
                }

                var selfieUrl = await _fileService.UploadFileAsync(
                    ConvertBase64ToIFormFile(request.SelfieImageBase64, "selfie.jpg"),
                    "kyc/selfies");

                // Update user with KYC information
                user.AadhaarNumber = request.DocumentType == "aadhaar" ? request.DocumentNumber : user.AadhaarNumber;
                user.PanNumber = request.DocumentType == "pan" ? request.DocumentNumber : user.PanNumber;
                user.AadhaarImageUrl = request.DocumentType == "aadhaar" ? frontImageUrl : user.AadhaarImageUrl;
                user.PanImageUrl = request.DocumentType == "pan" ? frontImageUrl : user.PanImageUrl;
                user.KycStatus = "InProgress";
                user.KycSubmittedAt = DateTime.UtcNow;
                user.IsKycVerified = false;
                user.KycRejectionReason = null;

                // Create detailed KYC record
                var kycRecord = new KycVerification
                {
                    UserId = userId.Value,
                    DocumentType = request.DocumentType,
                    DocumentNumber = request.DocumentNumber,
                    FrontImageUrl = frontImageUrl,
                    BackImageUrl = backImageUrl,
                    SelfieImageUrl = selfieUrl,
                    ExtractedData = JsonSerializer.Serialize(request.ExtractedData),
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Pending",
                    VerificationScore = 0.0f
                };

                _context.KycVerifications.Add(kycRecord);
                await _context.SaveChangesAsync();

                // Create audit log
                var auditLog = new KycAuditLog
                {
                    UserId = userId.Value,
                    Action = "KYC_SUBMITTED",
                    Details = $"Complete KYC submitted for document type: {request.DocumentType}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };

                _context.KycAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Send KYC notification email to admin (MVP approach)
                await SendKycNotificationEmailToAdmin(user, kycRecord, request.ExtractedData);

                // Return updated user data
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    IsKycVerified = user.IsKycVerified,
                    KycStatus = user.KycStatus,
                    AadhaarNumber = user.AadhaarNumber,
                    AadhaarImageUrl = user.AadhaarImageUrl,
                    PanNumber = user.PanNumber,
                    PanImageUrl = user.PanImageUrl,
                    KycSubmittedAt = user.KycSubmittedAt,
                    KycVerifiedAt = user.KycVerifiedAt,
                    KycRejectionReason = user.KycRejectionReason
                };

                return Ok(ApiResponse<UserDto>.SuccessResponse(
                    userDto, 
                    "KYC submitted successfully and is under review"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting KYC for user");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse(
                    "An error occurred while submitting KYC"));
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult<ApiResponse<KycStatusDto>>> GetKycStatus()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<KycStatusDto>.ErrorResponse("Unauthorized"));
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(ApiResponse<KycStatusDto>.ErrorResponse("User not found"));
                }

                var kycRecords = await _context.KycVerifications
                    .Where(k => k.UserId == userId.Value)
                    .OrderByDescending(k => k.SubmittedAt)
                    .ToListAsync();

                var statusDto = new KycStatusDto
                {
                    IsKycVerified = user.IsKycVerified,
                    KycStatus = user.KycStatus ?? "NotStarted",
                    SubmittedAt = user.KycSubmittedAt,
                    VerifiedAt = user.KycVerifiedAt,
                    RejectionReason = user.KycRejectionReason,
                    Documents = kycRecords.Select(k => new KycDocumentStatusDto
                    {
                        DocumentType = k.DocumentType,
                        Status = k.Status,
                        SubmittedAt = k.SubmittedAt,
                        VerificationScore = k.VerificationScore
                    }).ToList()
                };

                return Ok(ApiResponse<KycStatusDto>.SuccessResponse(
                    statusDto, 
                    "KYC status retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving KYC status for user");
                return StatusCode(500, ApiResponse<KycStatusDto>.ErrorResponse(
                    "An error occurred while retrieving KYC status"));
            }
        }

        [HttpPost("admin/approve/{userId}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<object>>> ApproveKyc(int userId, [FromBody] KycApprovalDto approval)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
                }

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
                    record.VerifiedBy = GetCurrentUserId();
                    record.AdminNotes = approval.AdminNotes;
                }

                // Create audit log
                var auditLog = new KycAuditLog
                {
                    UserId = userId,
                    Action = "KYC_APPROVED",
                    Details = $"KYC approved by admin. Notes: {approval.AdminNotes}",
                    Timestamp = DateTime.UtcNow,
                    AdminUserId = GetCurrentUserId(),
                    IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };

                _context.KycAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Send approval email to shop owner
                await SendKycApprovalEmailToUser(user, approval.AdminNotes);

                return Ok(ApiResponse<object>.SuccessResponse(
                    new { UserId = userId, Status = "Approved" }, 
                    "KYC approved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving KYC for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while approving KYC"));
            }
        }

        [HttpPost("admin/reject/{userId}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<ApiResponse<object>>> RejectKyc(int userId, [FromBody] KycRejectionDto rejection)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
                }

                user.IsKycVerified = false;
                user.KycStatus = "Rejected";
                user.KycRejectionReason = rejection.Reason;

                // Update KYC records
                var kycRecords = await _context.KycVerifications
                    .Where(k => k.UserId == userId)
                    .ToListAsync();

                foreach (var record in kycRecords)
                {
                    record.Status = "Rejected";
                    record.RejectionReason = rejection.Reason;
                    record.VerifiedBy = GetCurrentUserId();
                    record.AdminNotes = rejection.AdminNotes;
                }

                // Create audit log
                var auditLog = new KycAuditLog
                {
                    UserId = userId,
                    Action = "KYC_REJECTED",
                    Details = $"KYC rejected by admin. Reason: {rejection.Reason}",
                    Timestamp = DateTime.UtcNow,
                    AdminUserId = GetCurrentUserId(),
                    IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };

                _context.KycAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Send rejection email to shop owner
                await SendKycRejectionEmailToUser(user, rejection.Reason, rejection.AdminNotes);

                return Ok(ApiResponse<object>.SuccessResponse(
                    new { UserId = userId, Status = "Rejected", Reason = rejection.Reason }, 
                    "KYC rejected successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting KYC for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while rejecting KYC"));
            }
        }

        // Helper methods
        private int? GetCurrentUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }

        private bool IsValidAadhaarNumber(string aadhaarNumber)
        {
            var cleanNumber = aadhaarNumber.Replace(" ", "").Replace("-", "");
            return Regex.IsMatch(cleanNumber, @"^\d{12}$");
        }

        private bool IsValidPanNumber(string panNumber)
        {
            return Regex.IsMatch(panNumber.ToUpper(), @"^[A-Z]{5}[0-9]{4}[A-Z]$");
        }

        private async Task StoreKycVerificationResult(int userId, string documentType, KycVerificationResponseDto result)
        {
            var existingRecord = await _context.KycVerifications
                .FirstOrDefaultAsync(k => k.UserId == userId && k.DocumentType == documentType);

            if (existingRecord != null)
            {
                existingRecord.Status = result.Success ? "Verified" : "Failed";
                existingRecord.VerificationScore = result.ConfidenceScore ?? 0.0f;
                existingRecord.ExtractedData = JsonSerializer.Serialize(result.ExtractedData);
            }
            else
            {
                var newRecord = new KycVerification
                {
                    UserId = userId,
                    DocumentType = documentType,
                    Status = result.Success ? "Verified" : "Failed",
                    VerificationScore = result.ConfidenceScore ?? 0.0f,
                    ExtractedData = JsonSerializer.Serialize(result.ExtractedData),
                    SubmittedAt = DateTime.UtcNow
                };
                _context.KycVerifications.Add(newRecord);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<List<string>> ValidateKycSubmission(CompleteKycSubmissionDto request, int userId)
        {
            var errors = new List<string>();

            // Validate document number format
            if (request.DocumentType == "aadhaar" && !IsValidAadhaarNumber(request.DocumentNumber))
            {
                errors.Add("Invalid Aadhaar number format");
            }
            else if (request.DocumentType == "pan" && !IsValidPanNumber(request.DocumentNumber))
            {
                errors.Add("Invalid PAN number format");
            }

            // Check for duplicate documents
            if (request.DocumentType == "aadhaar")
            {
                var existingAadhaar = await _context.Users
                    .AnyAsync(u => u.AadhaarNumber == request.DocumentNumber && u.Id != userId);
                if (existingAadhaar)
                {
                    errors.Add("Aadhaar number already in use");
                }
            }
            else if (request.DocumentType == "pan")
            {
                var existingPan = await _context.Users
                    .AnyAsync(u => u.PanNumber == request.DocumentNumber && u.Id != userId);
                if (existingPan)
                {
                    errors.Add("PAN number already in use");
                }
            }

            return errors;
        }
        
        private IFormFile ConvertBase64ToIFormFile(string base64String, string fileName)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64String);
                var stream = new MemoryStream(bytes);
                return new FormFile(stream, 0, bytes.Length, "file", fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/jpeg"
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid base64 string format");
            }
        }

        private async Task SendKycNotificationEmailToAdmin(User user, KycVerification kycRecord, object extractedData)
        {
            try
            {
                var subject = $"🔔 New KYC Submission - {user.FirstName} {user.LastName}";
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                
                var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>New KYC Submission</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; }}
                        .email-container {{ max-width: 800px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; overflow: hidden; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white; }}
                        .content {{ padding: 30px; background-color: #f9f9f9; }}
                        .kyc-details {{ background-color: white; padding: 25px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                        .detail-row {{ display: flex; margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
                        .detail-label {{ font-weight: bold; width: 150px; color: #555; }}
                        .detail-value {{ color: #333; }}
                        .action-buttons {{ text-align: center; margin: 30px 0; }}
                        .btn {{ display: inline-block; padding: 15px 30px; margin: 10px; text-decoration: none; border-radius: 5px; font-weight: bold; text-align: center; }}
                        .btn-approve {{ background-color: #28a745; color: white; }}
                        .btn-reject {{ background-color: #dc3545; color: white; }}
                        .btn:hover {{ opacity: 0.9; }}
                        .images-section {{ margin: 20px 0; }}
                        .image-links {{ background-color: #e3f2fd; padding: 15px; border-radius: 5px; }}
                        .footer {{ padding: 20px; text-align: center; background-color: #f1f1f1; font-size: 12px; color: #666; }}
                        .alert {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <div class='header'>
                            <h1>🆔 New KYC Submission</h1>
                            <p>A shop owner has submitted their KYC documents for verification</p>
                        </div>
                        
                        <div class='content'>
                            <div class='alert'>
                                <strong>⚡ Action Required:</strong> Please review the submitted documents and approve or reject this KYC submission.
                            </div>
                            
                            <div class='kyc-details'>
                                <h3>👤 User Information</h3>
                                <div class='detail-row'>
                                    <div class='detail-label'>User ID:</div>
                                    <div class='detail-value'>#{user.Id}</div>
                                </div>
                                <div class='detail-row'>
                                    <div class='detail-label'>Name:</div>
                                    <div class='detail-value'>{user.FirstName} {user.LastName}</div>
                                </div>
                                <div class='detail-row'>
                                    <div class='detail-label'>Email:</div>
                                    <div class='detail-value'>{user.Email}</div>
                                </div>
                                <div class='detail-row'>
                                    <div class='detail-label'>Phone:</div>
                                    <div class='detail-value'>{user.PhoneNumber}</div>
                                </div>
                                <div class='detail-row'>
                                    <div class='detail-label'>Submitted:</div>
                                    <div class='detail-value'>{kycRecord.SubmittedAt:dd/MM/yyyy HH:mm}</div>
                                </div>
                            </div>
                            
                            <div class='kyc-details'>
                                <h3>📄 Document Information</h3>
                                <div class='detail-row'>
                                    <div class='detail-label'>Document Type:</div>
                                    <div class='detail-value'>{kycRecord.DocumentType.ToUpper()}</div>
                                </div>
                                <div class='detail-row'>
                                    <div class='detail-label'>Document Number:</div>
                                    <div class='detail-value'>{kycRecord.DocumentNumber}</div>
                                </div>
                                <div class='detail-row'>
                                    <div class='detail-label'>Status:</div>
                                    <div class='detail-value'><span style='background-color: #ffeaa7; padding: 3px 8px; border-radius: 3px;'>{kycRecord.Status}</span></div>
                                </div>
                            </div>
                            
                            <div class='kyc-details'>
                                <h3>🖼️ Submitted Documents</h3>
                                <div class='images-section'>
                                    <div class='image-links'>
                                        <p><strong>📷 Document Images:</strong></p>
                                        <p>• <a href='{kycRecord.FrontImageUrl}' target='_blank'>Front Document Image</a></p>
                                        {(string.IsNullOrEmpty(kycRecord.BackImageUrl) ? "" : $"<p>• <a href='{kycRecord.BackImageUrl}' target='_blank'>Back Document Image</a></p>")}
                                        <p>• <a href='{kycRecord.SelfieImageUrl}' target='_blank'>Selfie Image</a></p>
                                    </div>
                                </div>
                            </div>
                            
                            <div class='action-buttons'>
                                <h3>🎯 Quick Actions</h3>
                                <p>Click the buttons below to approve or reject this KYC submission:</p>
                                
                                <a href='{baseUrl}/admin/kyc/approve/{user.Id}?token=admin_access_token' class='btn btn-approve'>
                                    ✅ APPROVE KYC
                                </a>
                                
                                <a href='{baseUrl}/admin/kyc/reject/{user.Id}?token=admin_access_token' class='btn btn-reject'>
                                    ❌ REJECT KYC
                                </a>
                            </div>
                            
                            <div class='kyc-details'>
                                <h4>📋 Extracted Data Preview:</h4>
                                <pre style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; overflow-x: auto; font-size: 12px;'>{JsonSerializer.Serialize(extractedData, new JsonSerializerOptions { WriteIndented = true })}</pre>
                            </div>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated email from Stibe KYC System</p>
                            <p>Please do not reply to this email. For support, contact the development team.</p>
                            <p>&copy; {DateTime.Now.Year} Stibe. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

                await _emailService.SendEmailAsync("official.tishnu@gmail.com", subject, body, true);
                _logger.LogInformation("KYC notification email sent to admin for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send KYC notification email to admin for user {UserId}", user.Id);
            }
        }

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
                            
                            <p>If you have any questions, feel free to contact our support team.</p>
                            
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
                        .resubmit-btn {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 15px 0; }}
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
                            
                            <div class='action-required'>
                                <h3>🔧 Action Required</h3>
                                <p>To complete your verification:</p>
                                <ul>
                                    <li>Review the feedback provided above</li>
                                    <li>Address the mentioned issues</li>
                                    <li>Resubmit your KYC documents with corrections</li>
                                </ul>
                                
                                <div style='text-align: center; margin-top: 20px;'>
                                    <a href='#' class='resubmit-btn'>📄 RESUBMIT KYC DOCUMENTS</a>
                                </div>
                            </div>
                            
                            <p><strong>Common Issues to Check:</strong></p>
                            <ul>
                                <li>Document images are clear and readable</li>
                                <li>All required information is visible</li>
                                <li>Documents are valid and not expired</li>
                                <li>Selfie clearly shows your face</li>
                                <li>Information matches across all documents</li>
                            </ul>
                            
                            <p>If you have any questions about this feedback, please contact our support team. We're here to help you complete the verification process.</p>
                            
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
}
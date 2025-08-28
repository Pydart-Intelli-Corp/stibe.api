using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.Otp;
using stibe.api.Models.Entities;
using stibe.api.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OtpController : ControllerBase
    {
        private readonly IOtpService _otpService;
        private readonly ILogger<OtpController> _logger;

        public OtpController(IOtpService otpService, ILogger<OtpController> logger)
        {
            _otpService = otpService;
            _logger = logger;
        }

        /// <summary>
        /// Send an OTP to the specified email for the given purpose
        /// </summary>
        [HttpPost("send")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<OtpResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OtpResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<OtpResponseDto>), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<ApiResponse<OtpResponseDto>>> SendOtp(SendOtpRequestDto request)
        {
            try
            {
                // Validate purpose
                if (!IsValidPurpose(request.Purpose))
                {
                    return BadRequest(ApiResponse<OtpResponseDto>.ErrorResponse(
                        "Invalid purpose. Valid purposes are: EMAIL_VERIFICATION, SHOP_ACCESS, PASSWORD_RESET, PHONE_VERIFICATION, TWO_FACTOR_AUTH"));
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _otpService.SendOtpAsync(request.Email, request.Purpose, ipAddress, userAgent);

                if (!result.Success)
                {
                    if (result.Message.Contains("wait") || result.NextAllowedAt.HasValue)
                    {
                        return StatusCode(429, new ApiResponse<OtpResponseDto>
                        {
                            Success = false,
                            Message = result.Message,
                            Data = result,
                            Errors = new List<string>()
                        });
                    }
                    return BadRequest(ApiResponse<OtpResponseDto>.ErrorResponse(result.Message));
                }

                _logger.LogInformation($"OTP sent successfully to {request.Email} for purpose: {request.Purpose}");
                return Ok(ApiResponse<OtpResponseDto>.SuccessResponse(result, "OTP sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending OTP to {request.Email}");
                return StatusCode(500, ApiResponse<OtpResponseDto>.ErrorResponse("An error occurred while sending OTP"));
            }
        }

        /// <summary>
        /// Verify an OTP code for the specified email and purpose
        /// </summary>
        [HttpPost("verify")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<OtpResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OtpResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<OtpResponseDto>>> VerifyOtp(VerifyOtpRequestDto request)
        {
            try
            {
                // Validate purpose
                if (!IsValidPurpose(request.Purpose))
                {
                    return BadRequest(ApiResponse<OtpResponseDto>.ErrorResponse(
                        "Invalid purpose. Valid purposes are: EMAIL_VERIFICATION, SHOP_ACCESS, PASSWORD_RESET, PHONE_VERIFICATION, TWO_FACTOR_AUTH"));
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _otpService.VerifyOtpAsync(request.Email, request.Code, request.Purpose, ipAddress, userAgent);

                if (!result.Success)
                {
                    _logger.LogWarning($"OTP verification failed for {request.Email} with purpose: {request.Purpose}");
                    return BadRequest(ApiResponse<OtpResponseDto>.ErrorResponse(result.Message));
                }

                _logger.LogInformation($"OTP verified successfully for {request.Email} with purpose: {request.Purpose}");
                return Ok(ApiResponse<OtpResponseDto>.SuccessResponse(result, "OTP verified successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying OTP for {request.Email}");
                return StatusCode(500, ApiResponse<OtpResponseDto>.ErrorResponse("An error occurred while verifying OTP"));
            }
        }

        /// <summary>
        /// Get the current OTP status for an email and purpose
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<OtpStatusDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OtpStatusDto>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<OtpStatusDto>>> GetOtpStatus(
            [FromQuery] string email, 
            [FromQuery] string purpose)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(purpose))
                {
                    return BadRequest(ApiResponse<OtpStatusDto>.ErrorResponse("Email and purpose are required"));
                }

                // Validate purpose
                if (!IsValidPurpose(purpose))
                {
                    return BadRequest(ApiResponse<OtpStatusDto>.ErrorResponse(
                        "Invalid purpose. Valid purposes are: EMAIL_VERIFICATION, SHOP_ACCESS, PASSWORD_RESET, PHONE_VERIFICATION, TWO_FACTOR_AUTH"));
                }

                var status = await _otpService.GetOtpStatusAsync(email, purpose);
                return Ok(ApiResponse<OtpStatusDto>.SuccessResponse(status, "OTP status retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting OTP status for {email}");
                return StatusCode(500, ApiResponse<OtpStatusDto>.ErrorResponse("An error occurred while retrieving OTP status"));
            }
        }

        /// <summary>
        /// Invalidate all pending OTPs for the specified email and purpose
        /// </summary>
        [HttpPost("invalidate")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> InvalidateOtps(
            [FromBody] InvalidateOtpRequestDto request)
        {
            try
            {
                // Validate purpose
                if (!IsValidPurpose(request.Purpose))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid purpose. Valid purposes are: EMAIL_VERIFICATION, SHOP_ACCESS, PASSWORD_RESET, PHONE_VERIFICATION, TWO_FACTOR_AUTH"));
                }

                var result = await _otpService.InvalidateOtpsAsync(request.Email, request.Purpose);
                
                if (!result)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Failed to invalidate OTPs"));
                }

                _logger.LogInformation($"OTPs invalidated for {request.Email} with purpose: {request.Purpose}");
                return Ok(ApiResponse<object>.SuccessResponse(new { }, "OTPs invalidated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invalidating OTPs for {request.Email}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while invalidating OTPs"));
            }
        }

        /// <summary>
        /// Admin endpoint to cleanup expired OTPs
        /// </summary>
        [HttpPost("cleanup")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> CleanupExpiredOtps()
        {
            try
            {
                var cleanedCount = await _otpService.CleanupExpiredOtpsAsync();
                
                _logger.LogInformation($"Cleaned up {cleanedCount} expired OTP records");
                return Ok(ApiResponse<object>.SuccessResponse(
                    new { cleanedCount }, 
                    $"Cleaned up {cleanedCount} expired OTP records"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired OTPs");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while cleaning up expired OTPs"));
            }
        }

        /// <summary>
        /// Get supported OTP purposes
        /// </summary>
        [HttpGet("purposes")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<object>> GetSupportedPurposes()
        {
            var purposes = new
            {
                EmailVerification = OtpEntity.PURPOSE_EMAIL_VERIFICATION,
                ShopAccess = OtpEntity.PURPOSE_SHOP_ACCESS,
                ShopDelete = OtpEntity.PURPOSE_SHOP_DELETE,
                PasswordReset = OtpEntity.PURPOSE_PASSWORD_RESET,
                PhoneVerification = OtpEntity.PURPOSE_PHONE_VERIFICATION,
                TwoFactorAuth = OtpEntity.PURPOSE_TWO_FACTOR_AUTH
            };

            return Ok(ApiResponse<object>.SuccessResponse(purposes, "Supported OTP purposes"));
        }

        private bool IsValidPurpose(string purpose)
        {
            var validPurposes = new[]
            {
                OtpEntity.PURPOSE_EMAIL_VERIFICATION,
                OtpEntity.PURPOSE_SHOP_ACCESS,
                OtpEntity.PURPOSE_SHOP_STATUS_CHANGE,
                OtpEntity.PURPOSE_SHOP_DEFAULT_CHANGE,
                OtpEntity.PURPOSE_SHOP_DELETE,
                OtpEntity.PURPOSE_PASSWORD_RESET,
                OtpEntity.PURPOSE_PHONE_VERIFICATION,
                OtpEntity.PURPOSE_TWO_FACTOR_AUTH
            };

            return validPurposes.Contains(purpose);
        }
    }

    public class InvalidateOtpRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Purpose { get; set; } = string.Empty;
    }
}

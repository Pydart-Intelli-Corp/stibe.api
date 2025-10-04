using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stibe.api.Models.DTOs;
using stibe.api.Services.Interfaces;
using stibe.api.Services;
using System.Security.Claims;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly IUserCouponService _userCouponService;
        private readonly ILogger<CouponController> _logger;

        public CouponController(ICouponService couponService, IUserCouponService userCouponService, ILogger<CouponController> logger)
        {
            _couponService = couponService;
            _userCouponService = userCouponService;
            _logger = logger;
        }

        /// <summary>
        /// Validate a coupon code
        /// </summary>
        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<ActionResult<CouponValidationResponseDto>> ValidateCoupon([FromBody] ValidateCouponRequestDto request)
        {
            try
            {
                _logger.LogInformation("Validating coupon: {CouponCode}", request.CouponCode);
                
                var result = await _couponService.ValidateCouponAsync(request);
                
                if (result.IsValid)
                {
                    _logger.LogInformation("Coupon validation successful: {CouponCode}, Savings: {Savings}", 
                        request.CouponCode, result.Savings);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Coupon validation failed: {CouponCode}, Error: {Error}", 
                        request.CouponCode, result.ErrorMessage);
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon: {CouponCode}", request.CouponCode);
                return StatusCode(500, new CouponValidationResponseDto
                {
                    IsValid = false,
                    CouponCode = request.CouponCode,
                    ErrorMessage = "An error occurred while validating the coupon"
                });
            }
        }

        /// <summary>
        /// Apply a coupon for a user
        /// </summary>
        [HttpPost("apply")]
        [Authorize]
        public async Task<ActionResult<CouponApplicationResponseDto>> ApplyCoupon([FromBody] ApplyCouponRequestDto request)
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid or missing user ID in token when applying coupon: {CouponCode}", request.CouponCode);
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                // Override the UserId from the token (security measure)
                request.UserId = userId;

                _logger.LogInformation("Applying coupon: {CouponCode} for user: {UserId}", request.CouponCode, userId);
                
                var result = await _couponService.ApplyCouponAsync(request);
                
                if (result.Applied)
                {
                    _logger.LogInformation("Coupon applied successfully: {CouponCode} for user: {UserId}, Savings: {Savings}", 
                        request.CouponCode, userId, result.Savings);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Coupon application failed: {CouponCode} for user: {UserId}, Error: {Error}", 
                        request.CouponCode, userId, result.ErrorMessage);
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying coupon: {CouponCode} for user: {UserId}", request.CouponCode, request.UserId);
                return StatusCode(500, new CouponApplicationResponseDto
                {
                    Applied = false,
                    CouponCode = request.CouponCode,
                    ErrorMessage = "An error occurred while applying the coupon"
                });
            }
        }

        /// <summary>
        /// Get available coupons for a specific purpose
        /// </summary>
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<ActionResult<List<CouponConfigDto>>> GetAvailableCoupons([FromQuery] string purpose = "SHOP_REGISTRATION")
        {
            try
            {
                _logger.LogInformation("Getting available coupons for purpose: {Purpose}", purpose);
                
                var coupons = await _couponService.GetAvailableCouponsAsync(purpose);
                
                _logger.LogInformation("Retrieved {Count} available coupons for purpose: {Purpose}", coupons.Count, purpose);
                return Ok(coupons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available coupons for purpose: {Purpose}", purpose);
                return StatusCode(500, new { message = "An error occurred while retrieving available coupons" });
            }
        }

        /// <summary>
        /// Get coupon system configuration
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public async Task<ActionResult<CouponSystemConfigDto>> GetCouponConfig()
        {
            try
            {
                _logger.LogInformation("Getting coupon system configuration");
                
                var config = await _couponService.GetCouponConfigAsync();
                
                _logger.LogInformation("Retrieved coupon system configuration, Enabled: {Enabled}, Available Coupons: {Count}", 
                    config.EnableCoupons, config.AvailableCoupons.Count);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coupon system configuration");
                return StatusCode(500, new { message = "An error occurred while retrieving coupon configuration" });
            }
        }

        /// <summary>
        /// Mark a coupon as used (typically called after successful payment)
        /// </summary>
        [HttpPost("mark-used")]
        [Authorize]
        public async Task<ActionResult> MarkCouponAsUsed([FromBody] MarkCouponUsedRequestDto request)
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid or missing user ID in token when marking coupon as used: {CouponCode}", request.CouponCode);
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                _logger.LogInformation("Marking coupon as used: {CouponCode} for user: {UserId}, PaymentId: {PaymentId}", 
                    request.CouponCode, userId, request.PaymentId);
                
                var success = await _couponService.MarkCouponAsUsedAsync(request.CouponCode, userId, request.PaymentId);
                
                if (success)
                {
                    _logger.LogInformation("Coupon marked as used successfully: {CouponCode} for user: {UserId}", 
                        request.CouponCode, userId);
                    return Ok(new { message = "Coupon marked as used successfully" });
                }
                else
                {
                    _logger.LogWarning("Failed to mark coupon as used: {CouponCode} for user: {UserId}", 
                        request.CouponCode, userId);
                    return BadRequest(new { message = "Failed to mark coupon as used" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking coupon as used: {CouponCode} for user: {UserId}", 
                    request.CouponCode, request.UserId);
                return StatusCode(500, new { message = "An error occurred while marking the coupon as used" });
            }
        }

        /// <summary>
        /// Generate account registration coupon for new users (99.9% OFF)
        /// </summary>
        [HttpPost("generate-account-registration")]
        [Authorize]
        public async Task<ActionResult<AccountRegistrationCouponResponseDto>> GenerateAccountRegistrationCoupon()
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid or missing user ID in token when generating account registration coupon");
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                // Get user email and phone from claims
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var userPhone = User.FindFirst("phone_number")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("User email not found in token for account registration coupon generation");
                    return BadRequest(new { message = "User email is required for coupon generation" });
                }

                _logger.LogInformation("Generating account registration coupon for user: {UserId} ({Email})", userId, userEmail);

                // Check if user can receive coupon
                var canReceiveCoupon = await _userCouponService.CanUserReceiveCouponAsync(userEmail, userPhone ?? "");
                if (!canReceiveCoupon)
                {
                    return BadRequest(new AccountRegistrationCouponResponseDto
                    {
                        Success = false,
                        Message = "You already have an account registration coupon",
                        CouponCode = null
                    });
                }

                // Generate user-specific coupon
                var userCoupon = await _userCouponService.GenerateUserCouponAsync(userId, userEmail, userPhone ?? "");
                if (userCoupon == null)
                {
                    return StatusCode(500, new AccountRegistrationCouponResponseDto
                    {
                        Success = false,
                        Message = "Failed to generate account registration coupon",
                        CouponCode = null
                    });
                }

                // Send coupon email
                var emailSent = await _userCouponService.SendCouponEmailAsync(userCoupon);
                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send coupon email to {Email} for account registration", userEmail);
                }

                _logger.LogInformation("Account registration coupon generated successfully: {CouponCode} for user: {UserId}", 
                    userCoupon.CouponCode, userId);

                return Ok(new AccountRegistrationCouponResponseDto
                {
                    Success = true,
                    Message = "Account registration coupon generated successfully! Check your email for details.",
                    CouponCode = userCoupon.CouponCode,
                    DiscountPercentage = 99.9m,
                    OriginalAmount = 3999.0m,
                    FinalAmount = 5.0m,
                    SavingsAmount = 3994.0m,
                    MaxShops = 2,
                    EmailSent = emailSent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating account registration coupon");
                return StatusCode(500, new AccountRegistrationCouponResponseDto
                {
                    Success = false,
                    Message = "An error occurred while generating the account registration coupon",
                    CouponCode = null
                });
            }
        }

        /// <summary>
        /// Calculate discounted amount for a coupon and original amount
        /// </summary>
        [HttpPost("calculate-discount")]
        [AllowAnonymous]
        public async Task<ActionResult<decimal>> CalculateDiscount([FromBody] CalculateDiscountRequestDto request)
        {
            try
            {
                _logger.LogInformation("Calculating discount for coupon: {CouponCode}, Original Amount: {OriginalAmount}", 
                    request.CouponCode, request.OriginalAmount);
                
                var discountedAmount = await _couponService.CalculateDiscountedAmountAsync(
                    request.CouponCode, request.OriginalAmount, request.Purpose);
                
                var savings = request.OriginalAmount - discountedAmount;
                
                _logger.LogInformation("Discount calculated for coupon: {CouponCode}, Final Amount: {FinalAmount}, Savings: {Savings}", 
                    request.CouponCode, discountedAmount, savings);
                
                return Ok(new
                {
                    originalAmount = request.OriginalAmount,
                    finalAmount = discountedAmount,
                    savings = savings,
                    couponCode = request.CouponCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating discount for coupon: {CouponCode}", request.CouponCode);
                return StatusCode(500, new { message = "An error occurred while calculating the discount" });
            }
        }
    }

    // Additional DTOs for controller actions
    public class MarkCouponUsedRequestDto
    {
        public string CouponCode { get; set; } = string.Empty;
        public string? PaymentId { get; set; }
        public int UserId { get; set; } // This will be overridden by the token
    }

    public class CalculateDiscountRequestDto
    {
        public string CouponCode { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public string Purpose { get; set; } = "SHOP_REGISTRATION";
    }

    public class AccountRegistrationCouponResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CouponCode { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? OriginalAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public decimal? SavingsAmount { get; set; }
        public int? MaxShops { get; set; }
        public bool? EmailSent { get; set; }
    }
}
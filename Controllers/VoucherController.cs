using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VoucherController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VoucherController> _logger;

        public VoucherController(ApplicationDbContext context, ILogger<VoucherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse<VoucherValidationResponseDto>>> ValidateVoucher([FromBody] ValidateVoucherRequestDto request)
        {
            try
            {
                _logger.LogInformation($"🎫 Validating voucher: {request.VoucherCode}");

                // For now, return a mock response since we don't have a voucher system implemented
                // In a real implementation, you would check against a Vouchers table in the database
                
                var validVouchers = new Dictionary<string, (decimal discount, string description)>
                {
                    { "NEWSHOP10", (0.10m, "10% off for new shop creation") },
                    { "WELCOME15", (0.15m, "15% welcome discount") },
                    { "SAVE20", (0.20m, "20% savings voucher") },
                    { "FIRST50", (0.50m, "50% off first shop") }
                };

                if (validVouchers.ContainsKey(request.VoucherCode.ToUpper()))
                {
                    var (discount, description) = validVouchers[request.VoucherCode.ToUpper()];
                    var discountAmount = request.Amount * discount;

                    var response = new VoucherValidationResponseDto
                    {
                        IsValid = true,
                        VoucherCode = request.VoucherCode.ToUpper(),
                        DiscountPercentage = discount,
                        DiscountAmount = discountAmount,
                        FinalAmount = request.Amount - discountAmount,
                        Description = description,
                        Message = $"Voucher applied successfully! You saved ₹{discountAmount:F2}"
                    };

                    _logger.LogInformation($"✅ Voucher {request.VoucherCode} validated successfully - {discount:P0} discount");
                    return Ok(ApiResponse<VoucherValidationResponseDto>.SuccessResponse(response, "Voucher is valid"));
                }
                else
                {
                    var response = new VoucherValidationResponseDto
                    {
                        IsValid = false,
                        VoucherCode = request.VoucherCode,
                        Message = "Invalid voucher code. Please check and try again."
                    };

                    _logger.LogWarning($"❌ Invalid voucher code: {request.VoucherCode}");
                    return Ok(ApiResponse<VoucherValidationResponseDto>.SuccessResponse(response, "Voucher validation completed"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating voucher: {request.VoucherCode}");
                return StatusCode(500, ApiResponse<VoucherValidationResponseDto>.ErrorResponse("An error occurred while validating the voucher"));
            }
        }

        [HttpGet("available")]
        public async Task<ActionResult<ApiResponse<List<AvailableVoucherDto>>>> GetAvailableVouchers()
        {
            try
            {
                _logger.LogInformation("📋 Getting available vouchers");

                // Mock available vouchers - in a real implementation, fetch from database
                var availableVouchers = new List<AvailableVoucherDto>
                {
                    new AvailableVoucherDto
                    {
                        Code = "NEWSHOP10",
                        Title = "New Shop Discount",
                        Description = "Get 10% off on your new shop creation",
                        DiscountPercentage = 0.10m,
                        IsActive = true,
                        ExpiryDate = DateTime.UtcNow.AddDays(30),
                        MinimumAmount = 1.0m
                    },
                    new AvailableVoucherDto
                    {
                        Code = "WELCOME15",
                        Title = "Welcome Offer",
                        Description = "15% welcome discount for new users",
                        DiscountPercentage = 0.15m,
                        IsActive = true,
                        ExpiryDate = DateTime.UtcNow.AddDays(60),
                        MinimumAmount = 1.0m
                    },
                    new AvailableVoucherDto
                    {
                        Code = "SAVE20",
                        Title = "Super Saver",
                        Description = "Save 20% on shop registration",
                        DiscountPercentage = 0.20m,
                        IsActive = true,
                        ExpiryDate = DateTime.UtcNow.AddDays(15),
                        MinimumAmount = 1.0m
                    }
                };

                _logger.LogInformation($"✅ Retrieved {availableVouchers.Count} available vouchers");
                return Ok(ApiResponse<List<AvailableVoucherDto>>.SuccessResponse(availableVouchers, "Available vouchers retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available vouchers");
                return StatusCode(500, ApiResponse<List<AvailableVoucherDto>>.ErrorResponse("An error occurred while retrieving vouchers"));
            }
        }
    }

    // DTOs for voucher system
    public class ValidateVoucherRequestDto
    {
        [Required]
        [StringLength(50)]
        public string VoucherCode { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public class VoucherValidationResponseDto
    {
        public bool IsValid { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class AvailableVoucherDto
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal MinimumAmount { get; set; }
    }
}
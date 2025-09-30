using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs;
using stibe.api.Models.Entities;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services.Implementations
{
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IUserCouponService _userCouponService;
        private readonly ILogger<CouponService> _logger;

        public CouponService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IUserCouponService userCouponService,
            ILogger<CouponService> logger)
        {
            _context = context;
            _configuration = configuration;
            _userCouponService = userCouponService;
            _logger = logger;
        }

        public async Task<CouponValidationResponseDto> ValidateCouponAsync(ValidateCouponRequestDto request)
        {
            try
            {
                _logger.LogInformation("Validating coupon: {CouponCode} for purpose: {Purpose}", request.CouponCode, request.Purpose);

                // Check if coupons are enabled
                if (!_configuration.GetValue<bool>("Coupons:EnableCoupons", false))
                {
                    return new CouponValidationResponseDto
                    {
                        IsValid = false,
                        ErrorMessage = "Coupon system is currently disabled"
                    };
                }

                // First check user-specific coupons (STIBE-XXXX-XXXX format)
                if (request.CouponCode.StartsWith("STIBE-") && request.UserEmail != null)
                {
                    var userCoupon = await _userCouponService.GetUserCouponAsync(request.CouponCode, request.UserEmail);
                    if (userCoupon != null)
                    {
                        // Check if user has reached shop limit
                        if (await _userCouponService.HasUserReachedShopLimitAsync(request.UserEmail, userCoupon.PhoneNumber))
                        {
                            return new CouponValidationResponseDto
                            {
                                IsValid = false,
                                CouponCode = request.CouponCode,
                                ErrorMessage = "You have reached the maximum limit of 2 shops for this coupon"
                            };
                        }

                        // Calculate percentage for user-specific coupon
                        var userCouponSavings = request.OriginalAmount - 5;
                        var userCouponPercentage = request.OriginalAmount > 0 ? Math.Round((userCouponSavings / request.OriginalAmount) * 100, 1) : 0;
                        
                        // Return success for user-specific coupon
                        return new CouponValidationResponseDto
                        {
                            IsValid = true,
                            CouponCode = request.CouponCode,
                            Description = "Exclusive Shop Registration Coupon",
                            DiscountType = "FIXED",
                            DiscountValue = userCouponSavings,
                            OriginalAmount = request.OriginalAmount,
                            DiscountedAmount = userCouponSavings,
                            FinalAmount = 5,
                            Savings = userCouponSavings,
                            DiscountPercentage = userCouponPercentage,
                            RemainingUsage = userCoupon.MaxUsageLimit - userCoupon.UsageCount
                        };
                    }
                }

                // Then check configuration coupons
                var coupons = _configuration.GetSection("Coupons:AvailableCoupons").Get<List<CouponConfigDto>>() ?? new();
                var coupon = coupons.FirstOrDefault(c => c.Code.Equals(request.CouponCode, StringComparison.OrdinalIgnoreCase));

                if (coupon == null)
                {
                    return new CouponValidationResponseDto
                    {
                        IsValid = false,
                        CouponCode = request.CouponCode,
                        ErrorMessage = "Invalid coupon code"
                    };
                }

                // Check UserType restrictions
                if (coupon.UserType == "NEW_USER" && request.UserEmail != null)
                {
                    // For ACCOUNT99 and other NEW_USER coupons, check if user hasn't reached the 2-shop limit
                    var hasReachedLimit = await _userCouponService.HasUserReachedShopLimitAsync(request.UserEmail, request.PhoneNumber ?? "");
                    if (hasReachedLimit)
                    {
                        return new CouponValidationResponseDto
                        {
                            IsValid = false,
                            CouponCode = request.CouponCode,
                            ErrorMessage = "You have reached the maximum usage limit for this coupon (2 shops per account)"
                        };
                    }
                }
                // For UserType "ALL", no additional restrictions apply

                // Check if coupon is active
                if (!coupon.IsActive)
                {
                    return new CouponValidationResponseDto
                    {
                        IsValid = false,
                        CouponCode = request.CouponCode,
                        ErrorMessage = "This coupon is no longer active"
                    };
                }

                // Check validity dates
                var now = DateTime.UtcNow;
                if (now < coupon.ValidFrom || now > coupon.ValidUntil)
                {
                    return new CouponValidationResponseDto
                    {
                        IsValid = false,
                        CouponCode = request.CouponCode,
                        ErrorMessage = "This coupon has expired or is not yet valid"
                    };
                }

                // Check if applicable for the purpose
                if (!coupon.ApplicableFor.Contains(request.Purpose))
                {
                    return new CouponValidationResponseDto
                    {
                        IsValid = false,
                        CouponCode = request.CouponCode,
                        ErrorMessage = "This coupon is not applicable for the selected service"
                    };
                }

                // Check minimum order amount
                if (coupon.MinimumOrderAmount > 0 && request.OriginalAmount < coupon.MinimumOrderAmount)
                {
                    return new CouponValidationResponseDto
                    {
                        IsValid = false,
                        CouponCode = request.CouponCode,
                        ErrorMessage = $"Minimum order amount of ₹{coupon.MinimumOrderAmount:F0} required for this coupon"
                    };
                }

                // Check usage count
                var currentUsage = await _context.CouponUsages
                    .Where(cu => cu.CouponCode.ToLower() == request.CouponCode.ToLower())
                    .CountAsync();

                if (currentUsage >= coupon.MaxUsageCount)
                {
                    return new CouponValidationResponseDto
                    {
                        IsValid = false,
                        CouponCode = request.CouponCode,
                        ErrorMessage = "This coupon has reached its usage limit"
                    };
                }

                // Calculate discounted amount
                decimal finalAmount = await CalculateDiscountedAmountAsync(request.CouponCode, request.OriginalAmount, request.Purpose);
                decimal savings = request.OriginalAmount - finalAmount;
                decimal discountPercentage = request.OriginalAmount > 0 ? Math.Round((savings / request.OriginalAmount) * 100, 1) : 0;

                return new CouponValidationResponseDto
                {
                    IsValid = true,
                    CouponCode = coupon.Code,
                    Description = coupon.Description,
                    DiscountType = coupon.DiscountType,
                    DiscountValue = coupon.DiscountValue,
                    OriginalAmount = request.OriginalAmount,
                    DiscountedAmount = coupon.DiscountValue,
                    FinalAmount = finalAmount,
                    Savings = savings,
                    DiscountPercentage = discountPercentage,
                    ValidUntil = coupon.ValidUntil,
                    RemainingUsage = coupon.MaxUsageCount - currentUsage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon: {CouponCode}", request.CouponCode);
                return new CouponValidationResponseDto
                {
                    IsValid = false,
                    CouponCode = request.CouponCode,
                    ErrorMessage = "An error occurred while validating the coupon"
                };
            }
        }

        public async Task<CouponApplicationResponseDto> ApplyCouponAsync(ApplyCouponRequestDto request)
        {
            try
            {
                _logger.LogInformation("Applying coupon: {CouponCode} for user: {UserId}", request.CouponCode, request.UserId);

                // First validate the coupon
                var validation = await ValidateCouponAsync(new ValidateCouponRequestDto
                {
                    CouponCode = request.CouponCode,
                    Purpose = request.Purpose,
                    OriginalAmount = request.OriginalAmount,
                    UserEmail = request.UserEmail,
                    PhoneNumber = request.PhoneNumber
                });

                if (!validation.IsValid)
                {
                    return new CouponApplicationResponseDto
                    {
                        Applied = false,
                        CouponCode = request.CouponCode,
                        ErrorMessage = validation.ErrorMessage
                    };
                }

                // Handle user-specific coupons (STIBE-XXXX-XXXX)
                if (request.CouponCode.StartsWith("STIBE-") && request.UserEmail != null)
                {
                    // Increment usage count for user-specific coupon
                    var success = await _userCouponService.IncrementShopUsageAsync(
                        request.CouponCode, 
                        request.UserEmail, 
                        request.PhoneNumber ?? "");

                    if (!success)
                    {
                        return new CouponApplicationResponseDto
                        {
                            Applied = false,
                            CouponCode = request.CouponCode,
                            ErrorMessage = "Failed to apply user-specific coupon"
                        };
                    }

                    // Create standard coupon usage record for tracking
                    var userCouponUsage = new CouponUsage
                    {
                        CouponCode = request.CouponCode,
                        UserId = request.UserId,
                        Purpose = request.Purpose,
                        OriginalAmount = request.OriginalAmount,
                        FinalAmount = 5, // User-specific coupon always results in ₹5
                        Savings = request.OriginalAmount - 5,
                        Status = "APPLIED",
                        AppliedAt = DateTime.UtcNow,
                        Notes = "User-specific shop registration coupon applied"
                    };

                    _context.CouponUsages.Add(userCouponUsage);
                    await _context.SaveChangesAsync();

                    var appliedSavings = request.OriginalAmount - 5;
                    var appliedPercentage = request.OriginalAmount > 0 ? Math.Round((appliedSavings / request.OriginalAmount) * 100, 1) : 0;
                    
                    return new CouponApplicationResponseDto
                    {
                        Applied = true,
                        CouponCode = request.CouponCode,
                        Description = "Exclusive Shop Registration Coupon",
                        OriginalAmount = request.OriginalAmount,
                        FinalAmount = 5,
                        Savings = appliedSavings,
                        DiscountPercentage = appliedPercentage,
                        AppliedAt = DateTime.UtcNow,
                        UserId = request.UserId,
                        Purpose = request.Purpose
                    };
                }

                // Check if user has already used this coupon
                var existingUsage = await _context.CouponUsages
                    .AnyAsync(cu => cu.CouponCode.ToLower() == request.CouponCode.ToLower() 
                                && cu.UserId == request.UserId 
                                && cu.Purpose == request.Purpose
                                && !cu.IsDeleted);

                if (existingUsage)
                {
                    return new CouponApplicationResponseDto
                    {
                        Applied = false,
                        CouponCode = request.CouponCode,
                        ErrorMessage = "You have already used this coupon"
                    };
                }

                // Create coupon usage record
                var couponUsage = new CouponUsage
                {
                    CouponCode = validation.CouponCode,
                    UserId = request.UserId,
                    Purpose = request.Purpose,
                    OriginalAmount = request.OriginalAmount,
                    FinalAmount = validation.FinalAmount,
                    Savings = validation.Savings,
                    Status = "APPLIED",
                    AppliedAt = DateTime.UtcNow,
                    Notes = $"Coupon applied: {validation.Description}"
                };

                _context.CouponUsages.Add(couponUsage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Coupon applied successfully: {CouponCode} for user: {UserId}, Savings: {Savings}", 
                    request.CouponCode, request.UserId, validation.Savings);

                return new CouponApplicationResponseDto
                {
                    Applied = true,
                    CouponCode = validation.CouponCode,
                    Description = validation.Description,
                    OriginalAmount = request.OriginalAmount,
                    FinalAmount = validation.FinalAmount,
                    Savings = validation.Savings,
                    DiscountPercentage = validation.DiscountPercentage,
                    AppliedAt = couponUsage.AppliedAt,
                    UserId = request.UserId,
                    Purpose = request.Purpose
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying coupon: {CouponCode} for user: {UserId}", request.CouponCode, request.UserId);
                return new CouponApplicationResponseDto
                {
                    Applied = false,
                    CouponCode = request.CouponCode,
                    ErrorMessage = "An error occurred while applying the coupon"
                };
            }
        }

        public async Task<bool> MarkCouponAsUsedAsync(string couponCode, int userId, string? paymentId = null)
        {
            try
            {
                // First, handle regular coupon usage tracking
                var couponUsage = await _context.CouponUsages
                    .FirstOrDefaultAsync(cu => cu.CouponCode.ToLower() == couponCode.ToLower() 
                                            && cu.UserId == userId 
                                            && cu.Status == "APPLIED"
                                            && !cu.IsDeleted);

                if (couponUsage != null)
                {
                    couponUsage.Status = "USED";
                    couponUsage.UsedAt = DateTime.UtcNow;
                    couponUsage.PaymentId = paymentId;
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Coupon marked as used: {CouponCode} for user: {UserId}, PaymentId: {PaymentId}", 
                        couponCode, userId, paymentId);
                }

                // For predefined coupons like ACCOUNT99, also handle shop usage increment and restriction
                if (couponCode.Equals("ACCOUNT99", StringComparison.OrdinalIgnoreCase) || 
                    !couponCode.StartsWith("STIBE-")) // Predefined coupons don't start with STIBE-
                {
                    // Get user info to handle shop usage tracking
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // Increment shop usage for this predefined coupon
                        var incrementSuccess = await _userCouponService.IncrementShopUsageAsync(
                            couponCode, user.Email, user.PhoneNumber ?? "");
                        
                        if (incrementSuccess)
                        {
                            _logger.LogInformation("Shop usage incremented for predefined coupon: {CouponCode} for user: {UserId}", 
                                couponCode, userId);
                            
                            // Check if user has reached the 2-shop limit and needs to be restricted
                            var hasReachedLimit = await _userCouponService.HasUserReachedShopLimitAsync(
                                user.Email, user.PhoneNumber ?? "");
                            
                            if (hasReachedLimit)
                            {
                                await _userCouponService.RestrictUserFromCouponAsync(
                                    user.Email, user.PhoneNumber ?? "", couponCode);
                                
                                _logger.LogInformation("User {Email} restricted from using coupon {CouponCode} - reached 2 shop limit", 
                                    user.Email, couponCode);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to increment shop usage for predefined coupon: {CouponCode} for user: {UserId}", 
                                couponCode, userId);
                        }
                    }
                }
                
                return couponUsage != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking coupon as used: {CouponCode} for user: {UserId}", couponCode, userId);
                return false;
            }
        }

        public async Task<List<CouponConfigDto>> GetAvailableCouponsAsync(string purpose = "SHOP_REGISTRATION")
        {
            try
            {
                if (!_configuration.GetValue<bool>("Coupons:EnableCoupons", false))
                {
                    return new List<CouponConfigDto>();
                }

                var coupons = _configuration.GetSection("Coupons:AvailableCoupons").Get<List<CouponConfigDto>>() ?? new();
                var now = DateTime.UtcNow;

                var availableCoupons = new List<CouponConfigDto>();

                foreach (var coupon in coupons)
                {
                    if (coupon.IsActive && 
                        now >= coupon.ValidFrom && 
                        now <= coupon.ValidUntil &&
                        coupon.ApplicableFor.Contains(purpose))
                    {
                        var currentUsage = await _context.CouponUsages
                            .Where(cu => cu.CouponCode.ToLower() == coupon.Code.ToLower())
                            .CountAsync();

                        if (currentUsage < coupon.MaxUsageCount)
                        {
                            coupon.CurrentUsageCount = currentUsage;
                            availableCoupons.Add(coupon);
                        }
                    }
                }

                return availableCoupons;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available coupons for purpose: {Purpose}", purpose);
                return new List<CouponConfigDto>();
            }
        }

        public async Task<CouponSystemConfigDto> GetCouponConfigAsync()
        {
            try
            {
                var config = new CouponSystemConfigDto
                {
                    EnableCoupons = _configuration.GetValue<bool>("Coupons:EnableCoupons", false),
                    DefaultDiscountedAmount = _configuration.GetValue<decimal>("Coupons:DefaultDiscountedAmount", 5.0m),
                    AvailableCoupons = await GetAvailableCouponsAsync()
                };

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coupon system configuration");
                return new CouponSystemConfigDto
                {
                    EnableCoupons = false,
                    DefaultDiscountedAmount = 0,
                    AvailableCoupons = new List<CouponConfigDto>()
                };
            }
        }

        public Task<decimal> CalculateDiscountedAmountAsync(string couponCode, decimal originalAmount, string purpose)
        {
            try
            {
                var coupons = _configuration.GetSection("Coupons:AvailableCoupons").Get<List<CouponConfigDto>>() ?? new();
                var coupon = coupons.FirstOrDefault(c => c.Code.Equals(couponCode, StringComparison.OrdinalIgnoreCase));

                if (coupon == null)
                {
                    return Task.FromResult(originalAmount);
                }

                decimal finalAmount;

                switch (coupon.DiscountType.ToUpper())
                {
                    case "FIXED_AMOUNT":
                        var fixedDiscount = coupon.DiscountValue;
                        if (coupon.MaximumDiscount > 0)
                        {
                            fixedDiscount = Math.Min(fixedDiscount, coupon.MaximumDiscount);
                        }
                        finalAmount = Math.Max(0, originalAmount - fixedDiscount);
                        break;
                    case "PERCENTAGE":
                        var percentageDiscount = (originalAmount * coupon.DiscountValue) / 100;
                        if (coupon.MaximumDiscount > 0)
                        {
                            percentageDiscount = Math.Min(percentageDiscount, coupon.MaximumDiscount);
                        }
                        finalAmount = Math.Max(0, originalAmount - percentageDiscount);
                        break;
                    case "SET_AMOUNT":
                        // Set to a specific amount (like ₹5 for shop registration)
                        finalAmount = _configuration.GetValue<decimal>("Coupons:DefaultDiscountedAmount", 5.0m);
                        break;
                    default:
                        finalAmount = originalAmount;
                        break;
                }

                return Task.FromResult(finalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating discounted amount for coupon: {CouponCode}", couponCode);
                return Task.FromResult(originalAmount);
            }
        }
    }
}
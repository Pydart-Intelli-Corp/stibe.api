using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.Entities;
using stibe.api.Models.DTOs;
using stibe.api.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace stibe.api.Services.Implementations
{
    public interface IEnhancedCouponGenerationService
    {
        Task<UserCouponUsage?> GeneratePersonalizedCouponAsync(int userId, string email, string phoneNumber, string? preferredCouponType = null);
        Task<UserCouponUsage?> GeneratePercentageBasedCouponAsync(int userId, string email, string phoneNumber, decimal percentage, string purpose = "SHOP_REGISTRATION");
        Task<UserCouponUsage?> GenerateFixedAmountCouponAsync(int userId, string email, string phoneNumber, decimal fixedAmount, string purpose = "SHOP_REGISTRATION");
        Task<UserCouponUsage?> GenerateSetAmountCouponAsync(int userId, string email, string phoneNumber, decimal setAmount, string purpose = "SHOP_REGISTRATION");
        Task<List<CouponConfigDto>> GetAvailablePercentageTemplatesAsync();
        Task<bool> SendPersonalizedCouponEmailAsync(UserCouponUsage userCoupon, CouponConfigDto couponTemplate);
    }

    public class EnhancedCouponGenerationService : IEnhancedCouponGenerationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<EnhancedCouponGenerationService> _logger;

        public EnhancedCouponGenerationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<EnhancedCouponGenerationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<UserCouponUsage?> GeneratePersonalizedCouponAsync(int userId, string email, string phoneNumber, string? preferredCouponType = null)
        {
            try
            {
                // Check if user already has a coupon
                var existingCoupon = await _context.UserCouponUsages
                    .FirstOrDefaultAsync(ucu => 
                        (ucu.Email == email || ucu.PhoneNumber == phoneNumber) && 
                        !ucu.IsDeleted);

                if (existingCoupon != null)
                {
                    return existingCoupon;
                }

                // Get available coupon templates from appsettings.json
                var availableTemplates = await GetAvailablePercentageTemplatesAsync();
                
                CouponConfigDto selectedTemplate;

                // Select coupon based on preference or randomly
                if (!string.IsNullOrEmpty(preferredCouponType))
                {
                    selectedTemplate = availableTemplates.FirstOrDefault(c => 
                        c.DiscountType.Equals(preferredCouponType, StringComparison.OrdinalIgnoreCase)) 
                        ?? availableTemplates.First();
                }
                else
                {
                    // Weighted selection: prefer higher percentage discounts for new users
                    var random = new Random();
                    var percentageCoupons = availableTemplates.Where(c => c.DiscountType == "PERCENTAGE").ToList();
                    var fixedCoupons = availableTemplates.Where(c => c.DiscountType == "FIXED_AMOUNT").ToList();
                    var setAmountCoupons = availableTemplates.Where(c => c.DiscountType == "SET_AMOUNT").ToList();

                    // 60% chance for percentage, 30% for fixed, 10% for set amount
                    var typeSelector = random.Next(1, 101);
                    if (typeSelector <= 60 && percentageCoupons.Any())
                    {
                        selectedTemplate = percentageCoupons[random.Next(percentageCoupons.Count)];
                    }
                    else if (typeSelector <= 90 && fixedCoupons.Any())
                    {
                        selectedTemplate = fixedCoupons[random.Next(fixedCoupons.Count)];
                    }
                    else
                    {
                        selectedTemplate = setAmountCoupons.Any() ? setAmountCoupons[random.Next(setAmountCoupons.Count)] : availableTemplates.First();
                    }
                }

                // Generate personalized coupon based on selected template
                return selectedTemplate.DiscountType.ToUpper() switch
                {
                    "PERCENTAGE" => await GeneratePercentageBasedCouponAsync(userId, email, phoneNumber, selectedTemplate.DiscountValue),
                    "FIXED_AMOUNT" => await GenerateFixedAmountCouponAsync(userId, email, phoneNumber, selectedTemplate.DiscountValue),
                    "SET_AMOUNT" => await GenerateSetAmountCouponAsync(userId, email, phoneNumber, selectedTemplate.DiscountValue),
                    _ => await GeneratePercentageBasedCouponAsync(userId, email, phoneNumber, 25) // Default 25%
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating personalized coupon for user {userId} ({email})");
                return null;
            }
        }

        public async Task<UserCouponUsage?> GeneratePercentageBasedCouponAsync(int userId, string email, string phoneNumber, decimal percentage, string purpose = "SHOP_REGISTRATION")
        {
            try
            {
                var couponCode = GenerateUniqueCouponCode();
                var originalAmount = _configuration.GetValue<decimal>("Payment:ShopRegistrationFee", 3999.0m);
                var discountAmount = (originalAmount * percentage) / 100;
                var finalAmount = Math.Max(5, originalAmount - discountAmount); // Minimum ₹5

                var userCoupon = new UserCouponUsage
                {
                    UserId = userId,
                    CouponCode = couponCode,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Purpose = purpose,
                    IssuedAt = DateTime.UtcNow,
                    MaxUsageLimit = 2,
                    CouponType = "PERCENTAGE",
                    DiscountValue = percentage,
                    OriginalAmount = originalAmount,
                    FinalAmount = finalAmount,
                    SavingsAmount = discountAmount
                };

                _context.UserCouponUsages.Add(userCoupon);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generated {percentage}% percentage coupon {couponCode} for user {userId} ({email})");
                return userCoupon;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating percentage coupon for user {userId} ({email})");
                return null;
            }
        }

        public async Task<UserCouponUsage?> GenerateFixedAmountCouponAsync(int userId, string email, string phoneNumber, decimal fixedAmount, string purpose = "SHOP_REGISTRATION")
        {
            try
            {
                var couponCode = GenerateUniqueCouponCode();
                var originalAmount = _configuration.GetValue<decimal>("Payment:ShopRegistrationFee", 3999.0m);
                var finalAmount = Math.Max(5, originalAmount - fixedAmount); // Minimum ₹5
                var percentage = originalAmount > 0 ? Math.Round((fixedAmount / originalAmount) * 100, 1) : 0;

                var userCoupon = new UserCouponUsage
                {
                    UserId = userId,
                    CouponCode = couponCode,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Purpose = purpose,
                    IssuedAt = DateTime.UtcNow,
                    MaxUsageLimit = 2,
                    CouponType = "FIXED_AMOUNT",
                    DiscountValue = fixedAmount,
                    OriginalAmount = originalAmount,
                    FinalAmount = finalAmount,
                    SavingsAmount = fixedAmount
                };

                _context.UserCouponUsages.Add(userCoupon);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generated ₹{fixedAmount} fixed amount coupon {couponCode} for user {userId} ({email})");
                return userCoupon;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating fixed amount coupon for user {userId} ({email})");
                return null;
            }
        }

        public async Task<UserCouponUsage?> GenerateSetAmountCouponAsync(int userId, string email, string phoneNumber, decimal setAmount, string purpose = "SHOP_REGISTRATION")
        {
            try
            {
                var couponCode = GenerateUniqueCouponCode();
                var originalAmount = _configuration.GetValue<decimal>("Payment:ShopRegistrationFee", 3999.0m);
                var savingsAmount = originalAmount - setAmount;
                var percentage = originalAmount > 0 ? Math.Round((savingsAmount / originalAmount) * 100, 1) : 0;

                var userCoupon = new UserCouponUsage
                {
                    UserId = userId,
                    CouponCode = couponCode,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Purpose = purpose,
                    IssuedAt = DateTime.UtcNow,
                    MaxUsageLimit = 2,
                    CouponType = "SET_AMOUNT",
                    DiscountValue = setAmount,
                    OriginalAmount = originalAmount,
                    FinalAmount = setAmount,
                    SavingsAmount = savingsAmount
                };

                _context.UserCouponUsages.Add(userCoupon);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generated set amount (₹{setAmount}) coupon {couponCode} for user {userId} ({email})");
                return userCoupon;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating set amount coupon for user {userId} ({email})");
                return null;
            }
        }

        public Task<List<CouponConfigDto>> GetAvailablePercentageTemplatesAsync()
        {
            var coupons = _configuration.GetSection("Coupons:AvailableCoupons").Get<List<CouponConfigDto>>() ?? new();
            var now = DateTime.UtcNow;

            return Task.FromResult(coupons.Where(c => 
                c.IsActive &&
                now >= c.ValidFrom &&
                now <= c.ValidUntil &&
                c.ApplicableFor.Contains("SHOP_REGISTRATION")
            ).ToList());
        }

        public async Task<bool> SendPersonalizedCouponEmailAsync(UserCouponUsage userCoupon, CouponConfigDto couponTemplate)
        {
            try
            {
                var discountDescription = userCoupon.CouponType?.ToUpper() switch
                {
                    "PERCENTAGE" => $"{userCoupon.DiscountValue}% OFF",
                    "FIXED_AMOUNT" => $"₹{userCoupon.DiscountValue} OFF",
                    "SET_AMOUNT" => $"Pay only ₹{userCoupon.FinalAmount}",
                    _ => "Special Discount"
                };

                var savingsText = userCoupon.CouponType?.ToUpper() switch
                {
                    "PERCENTAGE" => $"You save {userCoupon.DiscountValue}% (₹{userCoupon.SavingsAmount:F0})",
                    "FIXED_AMOUNT" => $"You save ₹{userCoupon.SavingsAmount:F0}",
                    "SET_AMOUNT" => $"You save ₹{userCoupon.SavingsAmount:F0} ({Math.Round((userCoupon.SavingsAmount ?? 0 / userCoupon.OriginalAmount ?? 1) * 100, 1)}% OFF!)",
                    _ => $"You save ₹{userCoupon.SavingsAmount:F0}"
                };

                var subject = $"🎉 Your Exclusive {discountDescription} STIBE Coupon is Ready!";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f8f9fa;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px; text-align: center; color: white; margin-bottom: 30px;'>
                            <h1 style='margin: 0; font-size: 28px;'>🎉 Welcome to STIBE!</h1>
                            <p style='margin: 10px 0 0 0; font-size: 18px; opacity: 0.9;'>Your Personalized Business Coupon</p>
                        </div>
                        
                        <div style='background: #fff; padding: 25px; border-radius: 8px; margin-bottom: 25px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <h2 style='color: #333; margin-top: 0; text-align: center;'>🏪 {discountDescription} Shop Registration</h2>
                            <p style='color: #555; font-size: 16px; line-height: 1.6; text-align: center;'>
                                Congratulations! Your email has been verified and we've generated a personalized coupon just for you!
                            </p>
                        </div>

                        <div style='background: #fff; border: 3px solid #28a745; padding: 25px; border-radius: 12px; text-align: center; margin-bottom: 25px; box-shadow: 0 4px 8px rgba(40, 167, 69, 0.2);'>
                            <h3 style='color: #28a745; margin-top: 0; font-size: 20px;'>Your Exclusive Coupon Code</h3>
                            <div style='background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%); padding: 20px; border-radius: 8px; margin: 15px 0; border: 2px dashed #28a745;'>
                                <code style='font-size: 28px; font-weight: bold; color: #dc3545; letter-spacing: 3px; display: block; margin: 10px 0;'>{userCoupon.CouponCode}</code>
                                <p style='color: #28a745; margin: 10px 0 0 0; font-weight: bold; font-size: 18px;'>{discountDescription}</p>
                            </div>
                            <p style='color: #666; margin: 0; font-size: 14px;'>Use this code during shop registration payment</p>
                        </div>

                        <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin-bottom: 25px;'>
                            <h4 style='color: #856404; margin-top: 0;'>💰 Your Amazing Savings!</h4>
                            <div style='display: flex; justify-content: space-between; margin: 15px 0;'>
                                <div style='text-align: center; flex: 1;'>
                                    <strong style='color: #856404; font-size: 18px;'>Regular Price</strong>
                                    <p style='color: #dc3545; font-size: 24px; margin: 5px 0;'>₹{userCoupon.OriginalAmount:F0}</p>
                                </div>
                                <div style='text-align: center; flex: 1;'>
                                    <strong style='color: #856404; font-size: 18px;'>Your Price</strong>
                                    <p style='color: #28a745; font-size: 24px; margin: 5px 0; font-weight: bold;'>₹{userCoupon.FinalAmount:F0}</p>
                                </div>
                            </div>
                            <div style='text-align: center; background: #28a745; color: white; padding: 10px; border-radius: 5px; margin-top: 15px;'>
                                <strong style='font-size: 16px;'>{savingsText}</strong>
                            </div>
                            <ul style='color: #856404; text-align: left; padding-left: 20px; margin-top: 15px;'>
                                <li><strong>Coupon Type:</strong> {userCoupon.CouponType?.Replace("_", " ") ?? "Special Discount"}</li>
                                <li><strong>Maximum Shops:</strong> {userCoupon.MaxUsageLimit} shops allowed</li>
                                <li><strong>Valid Until:</strong> December 31, 2025</li>
                            </ul>
                        </div>

                        <div style='background: white; border: 1px solid #dee2e6; padding: 20px; border-radius: 8px; margin-bottom: 25px;'>
                            <h4 style='color: #0c5460; margin-top: 0;'>📋 How to Use Your Personalized Coupon</h4>
                            <ol style='color: #0c5460; text-align: left; padding-left: 20px; line-height: 1.8;'>
                                <li>Open the <strong>STIBE app</strong> and go to Shop Registration</li>
                                <li>Fill in your shop details (name, location, services, etc.)</li>
                                <li>At the payment screen, enter your coupon code: <strong style='color: #dc3545;'>{userCoupon.CouponCode}</strong></li>
                                <li>Watch the price drop from ₹{userCoupon.OriginalAmount:F0} to ₹{userCoupon.FinalAmount:F0}!</li>
                                <li>Complete your payment and start your business journey</li>
                            </ol>
                        </div>

                        <div style='background: #e7f3ff; border: 1px solid #b8daff; padding: 20px; border-radius: 8px; margin-bottom: 25px;'>
                            <h4 style='color: #004085; margin-top: 0;'>🔒 Security & Terms</h4>
                            <ul style='color: #004085; text-align: left; padding-left: 20px; font-size: 14px; line-height: 1.6;'>
                                <li>This coupon is exclusively linked to your email: <strong>{userCoupon.Email}</strong></li>
                                <li>Valid for your registered phone number: <strong>{userCoupon.PhoneNumber}</strong></li>
                                <li>Can be used for up to <strong>{userCoupon.MaxUsageLimit} shop registrations</strong></li>
                                <li>Cannot be combined with other offers</li>
                                <li>Non-transferable and non-refundable</li>
                            </ul>
                        </div>

                        <div style='text-align: center; margin: 30px 0;'>
                            <div style='background: #17a2b8; color: white; padding: 15px; border-radius: 8px; display: inline-block;'>
                                <p style='margin: 0; font-size: 16px; font-weight: bold;'>🚀 Ready to start your business journey?</p>
                                <p style='margin: 5px 0 0 0; font-size: 14px; opacity: 0.9;'>Download the STIBE app and use your coupon today!</p>
                            </div>
                        </div>

                        <div style='background: #343a40; color: white; padding: 20px; border-radius: 8px; text-align: center;'>
                            <p style='margin: 0; font-size: 16px;'>Welcome to the STIBE family! 🎉</p>
                            <p style='margin: 10px 0 0 0; font-size: 14px; opacity: 0.8;'>Build your business with confidence</p>
                        </div>

                        <div style='text-align: center; margin-top: 20px;'>
                            <p style='color: #6c757d; font-size: 12px;'>
                                Need help? Contact us at support@stibe.app<br>
                                © {DateTime.Now.Year} STIBE - Your Business Growth Partner
                            </p>
                        </div>
                    </body>
                    </html>";

                var result = await _emailService.SendEmailAsync(userCoupon.Email, subject, body);

                if (result)
                {
                    userCoupon.IsEmailSent = true;
                    userCoupon.EmailSentAt = DateTime.UtcNow;
                    userCoupon.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending personalized coupon email to {userCoupon.Email}");
                return false;
            }
        }

        private string GenerateUniqueCouponCode()
        {
            // Generate format: STIBE-XXXX-XXXX
            var random = new Random();
            var part1 = random.Next(1000, 9999);
            var part2 = random.Next(1000, 9999);
            return $"STIBE-{part1}-{part2}";
        }
    }
}
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.Entities;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services
{
    public interface IUserCouponService
    {
        Task<UserCouponUsage?> GenerateUserCouponAsync(int userId, string email, string phoneNumber);
        Task<bool> CanUserReceiveCouponAsync(string email, string phoneNumber);
        Task<bool> HasUserReachedShopLimitAsync(string email, string phoneNumber);
        Task<bool> IncrementShopUsageAsync(string couponCode, string email, string phoneNumber);
        Task<UserCouponUsage?> GetUserCouponAsync(string couponCode, string email);
        Task<bool> SendCouponEmailAsync(UserCouponUsage userCoupon);
        Task<UserCouponUsage?> AssignPredefinedCouponAsync(int userId, string email, string phoneNumber, string couponCode);
        Task<bool> SendPredefinedCouponEmailAsync(UserCouponUsage userCouponUsage, string couponCode);
        Task<bool> RestrictUserFromCouponAsync(string email, string phoneNumber, string couponCode);
        Task<bool> CanUserUseCouponAsync(string email, string phoneNumber, string couponCode);
    }

    public class UserCouponService : IUserCouponService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserCouponService> _logger;

        public UserCouponService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<UserCouponService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<UserCouponUsage?> GenerateUserCouponAsync(int userId, string email, string phoneNumber)
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

                // Generate unique coupon code
                var couponCode = GenerateUniqueCouponCode();

                var userCoupon = new UserCouponUsage
                {
                    UserId = userId,
                    CouponCode = couponCode,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Purpose = "SHOP_REGISTRATION",
                    IssuedAt = DateTime.UtcNow,
                    MaxUsageLimit = 2,
                    CouponType = "SET_AMOUNT",
                    DiscountValue = 99.9m,
                    OriginalAmount = 3999.0m,
                    FinalAmount = 5.0m,
                    SavingsAmount = 3994.0m
                };

                _context.UserCouponUsages.Add(userCoupon);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generated coupon {couponCode} for user {userId} ({email})");
                return userCoupon;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating coupon for user {userId} ({email})");
                return null;
            }
        }

        public async Task<bool> CanUserReceiveCouponAsync(string email, string phoneNumber)
        {
            var existingCoupon = await _context.UserCouponUsages
                .FirstOrDefaultAsync(ucu => 
                    (ucu.Email == email || ucu.PhoneNumber == phoneNumber) && 
                    !ucu.IsDeleted);

            return existingCoupon == null;
        }

        public async Task<bool> HasUserReachedShopLimitAsync(string email, string phoneNumber)
        {
            var userCoupon = await _context.UserCouponUsages
                .FirstOrDefaultAsync(ucu => 
                    (ucu.Email == email || ucu.PhoneNumber == phoneNumber) && 
                    !ucu.IsDeleted);

            if (userCoupon == null) return false;

            return userCoupon.UsageCount >= userCoupon.MaxUsageLimit || userCoupon.IsBlocked;
        }

        public async Task<bool> IncrementShopUsageAsync(string couponCode, string email, string phoneNumber)
        {
            try
            {
                var userCoupon = await _context.UserCouponUsages
                    .FirstOrDefaultAsync(ucu => 
                        ucu.CouponCode == couponCode && 
                        (ucu.Email == email || ucu.PhoneNumber == phoneNumber) &&
                        !ucu.IsDeleted);

                if (userCoupon == null) return false;

                userCoupon.UsageCount++;
                userCoupon.UpdatedAt = DateTime.UtcNow;

                // Block if reached limit
                if (userCoupon.UsageCount >= userCoupon.MaxUsageLimit)
                {
                    userCoupon.IsBlocked = true;
                    userCoupon.BlockedAt = DateTime.UtcNow;
                    userCoupon.BlockReason = "Maximum shop limit reached";
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error incrementing usage for coupon {couponCode}");
                return false;
            }
        }

        public async Task<UserCouponUsage?> GetUserCouponAsync(string couponCode, string email)
        {
            return await _context.UserCouponUsages
                .FirstOrDefaultAsync(ucu => 
                    ucu.CouponCode == couponCode && 
                    ucu.Email == email &&
                    !ucu.IsDeleted);
        }

        public async Task<bool> SendCouponEmailAsync(UserCouponUsage userCoupon)
        {
            try
            {
                var subject = "🎉 Welcome to STIBE! 99% OFF Account Registration - 2 Shops for ₹5 Each!";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px; text-align: center; color: white; margin-bottom: 30px;'>
                            <h1 style='margin: 0; font-size: 28px;'>🎉 Welcome to STIBE!</h1>
                            <p style='margin: 10px 0 0 0; font-size: 18px; opacity: 0.9;'>99% OFF Account Registration Special!</p>
                        </div>
                        
                        <div style='background: #f8f9fa; padding: 25px; border-radius: 8px; margin-bottom: 25px;'>
                            <h2 style='color: #333; margin-top: 0;'>🏪 Account Registration - 99% OFF!</h2>
                            <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                                🎊 <strong>INCREDIBLE OFFER!</strong> Your account has been verified and you've unlocked our 
                                <span style='color: #dc3545; font-weight: bold; font-size: 18px;'>99% DISCOUNT</span> 
                                for account registration! Register up to <strong>2 shops</strong> for just <strong>₹5 each</strong>!
                            </p>
                        </div>

                        <div style='background: #fff; border: 2px solid #28a745; padding: 25px; border-radius: 8px; text-align: center; margin-bottom: 25px;'>
                            <h3 style='color: #28a745; margin-top: 0;'>Your Exclusive Coupon Code</h3>
                            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                                <code style='font-size: 24px; font-weight: bold; color: #dc3545; letter-spacing: 2px;'>{userCoupon.CouponCode}</code>
                            </div>
                            <p style='color: #666; margin: 0;'>Use this code during shop registration payment</p>
                        </div>

                        <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin-bottom: 25px;'>
                            <h4 style='color: #856404; margin-top: 0;'>💰 MASSIVE 99% SAVINGS!</h4>
                            <div style='display: flex; justify-content: space-between; align-items: center; margin: 15px 0;'>
                                <div style='text-align: center; flex: 1;'>
                                    <p style='color: #856404; margin: 0; font-size: 14px;'><strong>Regular Price</strong></p>
                                    <p style='color: #dc3545; font-size: 28px; margin: 5px 0; text-decoration: line-through;'>₹3,999</p>
                                </div>
                                <div style='font-size: 40px; color: #28a745; flex: 0.3; text-align: center;'>→</div>
                                <div style='text-align: center; flex: 1;'>
                                    <p style='color: #856404; margin: 0; font-size: 14px;'><strong>Your Price</strong></p>
                                    <p style='color: #28a745; font-size: 32px; margin: 5px 0; font-weight: bold;'>₹5</p>
                                </div>
                            </div>
                            <div style='background: #28a745; color: white; padding: 15px; border-radius: 8px; text-align: center; margin: 15px 0;'>
                                <p style='margin: 0; font-size: 20px; font-weight: bold;'>🎉 YOU SAVE ₹3,994 PER SHOP!</p>
                                <p style='margin: 5px 0 0 0; font-size: 16px;'>That's 99.9% OFF the regular price!</p>
                            </div>
                            <ul style='color: #856404; text-align: left; padding-left: 20px;'>
                                <li><strong>Account Registration:</strong> 99% Discount Applied</li>
                                <li><strong>Maximum Shops:</strong> 2 shops with this offer</li>
                                <li><strong>Total Savings:</strong> Up to ₹7,988 (for 2 shops)</li>
                            </ul>
                        </div>

                        <div style='background: #d1ecf1; border: 1px solid #bee5eb; padding: 20px; border-radius: 8px; margin-bottom: 25px;'>
                            <h4 style='color: #0c5460; margin-top: 0;'>📋 How to Use Your Coupon</h4>
                            <ol style='color: #0c5460; text-align: left; padding-left: 20px;'>
                                <li>Go to the Shop Registration section in the STIBE app</li>
                                <li>Fill in your shop details</li>
                                <li>At the payment screen, enter your coupon code: <strong>{userCoupon.CouponCode}</strong></li>
                                <li>Watch the price drop from ₹3,999 to just ₹5!</li>
                                <li>Complete your payment and start your business journey</li>
                            </ol>
                        </div>

                        <div style='text-align: center; margin: 30px 0;'>
                            <p style='color: #666; font-size: 14px;'>
                                This coupon is valid for your email ({userCoupon.Email}) and registered phone number only.<br>
                                You can register up to 2 shops with this special offer.
                            </p>
                        </div>

                        <div style='background: #343a40; color: white; padding: 20px; border-radius: 8px; text-align: center;'>
                            <p style='margin: 0; font-size: 16px;'>Welcome to the STIBE family! 🚀</p>
                            <p style='margin: 10px 0 0 0; font-size: 14px; opacity: 0.8;'>Build your business with confidence</p>
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
                _logger.LogError(ex, $"Error sending coupon email to {userCoupon.Email}");
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

        public async Task<UserCouponUsage?> AssignPredefinedCouponAsync(int userId, string email, string phoneNumber, string couponCode)
        {
            try
            {
                // Check if user already has this coupon assigned
                var existingCoupon = await _context.UserCouponUsages
                    .FirstOrDefaultAsync(ucu => 
                        (ucu.Email == email || ucu.PhoneNumber == phoneNumber) && 
                        ucu.CouponCode == couponCode &&
                        !ucu.IsDeleted);

                if (existingCoupon != null)
                {
                    return existingCoupon;
                }

                var userCoupon = new UserCouponUsage
                {
                    UserId = userId,
                    CouponCode = couponCode,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Purpose = "SHOP_REGISTRATION",
                    IssuedAt = DateTime.UtcNow,
                    MaxUsageLimit = 2,
                    CouponType = "PREDEFINED",
                    DiscountValue = 99.0m,
                    OriginalAmount = 3999.0m,
                    FinalAmount = 39.99m, // 99% off = ₹39.99
                    SavingsAmount = 3959.01m
                };

                _context.UserCouponUsages.Add(userCoupon);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Assigned predefined coupon {couponCode} to user {userId} ({email})");
                return userCoupon;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning predefined coupon {couponCode} to user {userId} ({email})");
                return null;
            }
        }

        public async Task<bool> SendPredefinedCouponEmailAsync(UserCouponUsage userCouponUsage, string couponCode)
        {
            try
            {
                var subject = $"🎉 Welcome to STIBE! Your {couponCode} Coupon - 99% OFF!";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px; text-align: center; color: white; margin-bottom: 30px;'>
                            <h1 style='margin: 0; font-size: 28px;'>🎉 Welcome to STIBE!</h1>
                            <p style='margin: 10px 0 0 0; font-size: 18px; opacity: 0.9;'>Account Registration - 99% OFF!</p>
                        </div>
                        
                        <div style='background: #f8f9fa; padding: 25px; border-radius: 8px; margin-bottom: 25px;'>
                            <h2 style='color: #333; margin-top: 0;'>🏪 Account Registration Special - 99% OFF!</h2>
                            <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                                🎊 <strong>AMAZING OFFER!</strong> Your account has been verified and you can now use the 
                                <span style='color: #dc3545; font-weight: bold; font-size: 18px;'>{couponCode}</span> 
                                coupon for <strong>99% discount</strong> on shop registration! Register up to <strong>2 shops</strong> with this incredible offer!
                            </p>
                        </div>

                        <div style='background: #fff; border: 3px solid #28a745; padding: 25px; border-radius: 12px; text-align: center; margin-bottom: 25px;'>
                            <h3 style='color: #28a745; margin-top: 0;'>Your Coupon Code</h3>
                            <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 15px 0; border: 2px dashed #28a745;'>
                                <code style='font-size: 32px; font-weight: bold; color: #dc3545; letter-spacing: 3px;'>{couponCode}</code>
                            </div>
                            <p style='color: #666; margin: 0;'>Use this code during shop registration payment</p>
                        </div>

                        <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin-bottom: 25px;'>
                            <h4 style='color: #856404; margin-top: 0;'>💰 INCREDIBLE 99% SAVINGS!</h4>
                            <div style='display: flex; justify-content: space-between; align-items: center; margin: 15px 0;'>
                                <div style='text-align: center; flex: 1;'>
                                    <p style='color: #856404; margin: 0; font-size: 14px;'><strong>Regular Price</strong></p>
                                    <p style='color: #dc3545; font-size: 28px; margin: 5px 0; text-decoration: line-through;'>₹3,999</p>
                                </div>
                                <div style='font-size: 40px; color: #28a745; flex: 0.3; text-align: center;'>→</div>
                                <div style='text-align: center; flex: 1;'>
                                    <p style='color: #856404; margin: 0; font-size: 14px;'><strong>Your Price</strong></p>
                                    <p style='color: #28a745; font-size: 32px; margin: 5px 0; font-weight: bold;'>₹39.99</p>
                                </div>
                            </div>
                            <div style='background: #28a745; color: white; padding: 15px; border-radius: 8px; text-align: center; margin: 15px 0;'>
                                <p style='margin: 0; font-size: 20px; font-weight: bold;'>🎉 YOU SAVE ₹3,959 PER SHOP!</p>
                                <p style='margin: 5px 0 0 0; font-size: 16px;'>That's 99% OFF the regular price!</p>
                            </div>
                            <ul style='color: #856404; text-align: left; padding-left: 20px;'>
                                <li><strong>Coupon Code:</strong> {couponCode}</li>
                                <li><strong>Maximum Shops:</strong> 2 shops with this coupon</li>
                                <li><strong>Total Savings:</strong> Up to ₹7,918 (for 2 shops)</li>
                                <li><strong>Restriction:</strong> After 2 shops, coupon will be automatically disabled for your account</li>
                            </ul>
                        </div>

                        <div style='background: #d1ecf1; border: 1px solid #bee5eb; padding: 20px; border-radius: 8px; margin-bottom: 25px;'>
                            <h4 style='color: #0c5460; margin-top: 0;'>📋 How to Use Your {couponCode} Coupon</h4>
                            <ol style='color: #0c5460; text-align: left; padding-left: 20px;'>
                                <li>Go to the Shop Registration section in the STIBE app</li>
                                <li>Fill in your shop details</li>
                                <li>At the payment screen, enter your coupon code: <strong>{couponCode}</strong></li>
                                <li>Watch the price drop from ₹3,999 to just ₹39.99!</li>
                                <li>Complete your payment and start your business journey</li>
                                <li><strong>Important:</strong> After registering 2 shops, this coupon will be automatically disabled for your account</li>
                            </ol>
                        </div>

                        <div style='text-align: center; margin: 30px 0;'>
                            <p style='color: #666; font-size: 14px;'>
                                This coupon is valid for your email ({userCouponUsage.Email}) and registered phone number only.<br>
                                You can register up to 2 shops with this special offer.<br>
                                <strong>After 2 shops are registered, this coupon will be automatically restricted.</strong>
                            </p>
                        </div>

                        <div style='background: #343a40; color: white; padding: 20px; border-radius: 8px; text-align: center;'>
                            <p style='margin: 0; font-size: 16px;'>Welcome to the STIBE family! 🚀</p>
                            <p style='margin: 10px 0 0 0; font-size: 14px; opacity: 0.8;'>Build your business with confidence</p>
                        </div>
                    </body>
                    </html>";

                var result = await _emailService.SendEmailAsync(userCouponUsage.Email, subject, body);

                if (result)
                {
                    userCouponUsage.IsEmailSent = true;
                    userCouponUsage.EmailSentAt = DateTime.UtcNow;
                    userCouponUsage.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending predefined coupon email to {userCouponUsage.Email}");
                return false;
            }
        }

        public async Task<bool> RestrictUserFromCouponAsync(string email, string phoneNumber, string couponCode)
        {
            try
            {
                var userCoupon = await _context.UserCouponUsages
                    .FirstOrDefaultAsync(ucu => 
                        (ucu.Email == email || ucu.PhoneNumber == phoneNumber) && 
                        ucu.CouponCode == couponCode &&
                        !ucu.IsDeleted);

                if (userCoupon != null)
                {
                    userCoupon.IsBlocked = true;
                    userCoupon.BlockedAt = DateTime.UtcNow;
                    userCoupon.BlockReason = "Maximum usage limit reached (2 shops)";
                    userCoupon.UpdatedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Restricted user {email} from using coupon {couponCode} - reached 2 shop limit");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restricting user {email} from coupon {couponCode}");
                return false;
            }
        }

        public async Task<bool> CanUserUseCouponAsync(string email, string phoneNumber, string couponCode)
        {
            try
            {
                var userCoupon = await _context.UserCouponUsages
                    .FirstOrDefaultAsync(ucu => 
                        (ucu.Email == email || ucu.PhoneNumber == phoneNumber) && 
                        ucu.CouponCode == couponCode &&
                        !ucu.IsDeleted);

                if (userCoupon == null)
                {
                    // User doesn't have this coupon assigned, can't use it
                    return false;
                }

                // Check if blocked or reached usage limit
                return !userCoupon.IsBlocked && userCoupon.UsageCount < userCoupon.MaxUsageLimit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user {email} can use coupon {couponCode}");
                return false;
            }
        }
    }
}
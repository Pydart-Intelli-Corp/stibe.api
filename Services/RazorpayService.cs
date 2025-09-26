using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using stibe.api.Data;
using stibe.api.Models.DTOs;
using stibe.api.Models.Entities;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace stibe.api.Services
{
    public interface IRazorpayService
    {
        Task<RazorpayOrderResponseDto> CreateOrderAsync(CreateRazorpayOrderRequestDto request);
        Task<PaymentVerificationResponseDto> VerifyPaymentAsync(VerifyRazorpayPaymentRequestDto request);
        Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string paymentId);
        Task<RefundResponseDto> CreateRefundAsync(RefundRequestDto request);
        Task<bool> ProcessWebhookAsync(RazorpayWebhookDto webhookData, string signature);
        bool VerifyWebhookSignature(string payload, string signature);
        PaymentConfigDto GetPaymentConfig();
    }

    public class RazorpayService : IRazorpayService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RazorpayService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICouponService _couponService;
        private readonly IPdfService _pdfService;
        private readonly IEmailService _emailService;
        private readonly IGstService _gstService;
        private readonly RazorpayClient _razorpayClient;
        private readonly string _keyId;
        private readonly string _keySecret;
        private readonly string _webhookSecret;

        public RazorpayService(
            ApplicationDbContext context, 
            ILogger<RazorpayService> logger,
            IConfiguration configuration,
            ICouponService couponService,
            IPdfService pdfService,
            IEmailService emailService,
            IGstService gstService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _couponService = couponService;
            _pdfService = pdfService;
            _emailService = emailService;
            _gstService = gstService;
            
            _keyId = _configuration["Razorpay:KeyId"] ?? throw new ArgumentException("Razorpay KeyId not configured");
            _keySecret = _configuration["Razorpay:KeySecret"] ?? throw new ArgumentException("Razorpay KeySecret not configured");
            _webhookSecret = _configuration["Razorpay:WebhookSecret"] ?? "";
            
            _razorpayClient = new RazorpayClient(_keyId, _keySecret);
        }

        public async Task<RazorpayOrderResponseDto> CreateOrderAsync(CreateRazorpayOrderRequestDto request)
        {
            try
            {
                _logger.LogInformation("Creating Razorpay order for user {UserId}, amount {Amount}, couponCode {CouponCode}", 
                    request.UserId, request.Amount, request.CouponCode);

                // Generate unique payment ID
                var paymentId = $"STIBE_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..50];
                
                // Apply coupon discount if provided
                decimal baseAmount = request.Amount;
                decimal discountAmount = 0;
                string? appliedCouponCode = null;
                
                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    try
                    {
                        var validationRequest = new ValidateCouponRequestDto
                        {
                            CouponCode = request.CouponCode,
                            Purpose = request.Purpose,
                            OriginalAmount = request.Amount,
                            UserEmail = request.ShopData?.Email,
                            PhoneNumber = request.ShopData?.PhoneNumber
                        };
                        
                        var couponValidation = await _couponService.ValidateCouponAsync(validationRequest);
                        
                        if (couponValidation.IsValid)
                        {
                            discountAmount = request.Amount - couponValidation.FinalAmount;
                            appliedCouponCode = request.CouponCode;
                            
                            _logger.LogInformation("Coupon applied successfully: {CouponCode}, Original: {OriginalAmount}, Discount: {DiscountAmount}", 
                                request.CouponCode, request.Amount, discountAmount);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid coupon code provided: {CouponCode}, Error: {Error}", 
                                request.CouponCode, couponValidation.ErrorMessage);
                            // Continue with original amount if coupon is invalid
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error validating coupon: {CouponCode}", request.CouponCode);
                        // Continue with original amount if coupon validation fails
                    }
                }
                
                // Calculate GST breakdown for the payment
                var gstBreakdown = _gstService.GetPaymentGstBreakdown(baseAmount, discountAmount, appliedCouponCode);
                var finalAmountWithGst = gstBreakdown.FinalAmount;
                
                _logger.LogInformation("GST Calculation: Base={BaseAmount}, Discount={DiscountAmount}, GST={GstAmount}, Final={FinalAmount}", 
                    gstBreakdown.BaseAmount, gstBreakdown.DiscountAmount, gstBreakdown.GstAmount, gstBreakdown.FinalAmount);
                
                // Convert amount to paisa (Razorpay expects amount in smallest currency unit)
                var amountInPaisa = (int)(finalAmountWithGst * 100);
                
                // Prepare notes with GST breakdown
                var orderNotes = new Dictionary<string, object>();
                
                // Add existing notes if any
                if (request.Notes != null)
                {
                    foreach (var note in request.Notes)
                    {
                        orderNotes[note.Key] = note.Value;
                    }
                }
                
                // Add GST breakdown
                orderNotes["original_amount"] = request.Amount.ToString("F2");
                orderNotes["base_amount"] = gstBreakdown.BaseAmount.ToString("F2");
                orderNotes["discount_applied"] = discountAmount.ToString("F2");
                orderNotes["gst_rate"] = gstBreakdown.GstRate.ToString("F1");
                orderNotes["gst_amount"] = gstBreakdown.GstAmount.ToString("F2");
                orderNotes["final_amount_with_gst"] = finalAmountWithGst.ToString("F2");
                
                if (!string.IsNullOrEmpty(appliedCouponCode))
                {
                    orderNotes["coupon_code"] = appliedCouponCode;
                }
                
                // Create Razorpay order
                var orderRequest = new Dictionary<string, object>
                {
                    { "amount", amountInPaisa },
                    { "currency", request.Currency },
                    { "receipt", request.Receipt ?? paymentId },
                    { "notes", orderNotes }
                };

                var order = _razorpayClient.Order.Create(orderRequest);
                
                // Store payment record in database
                var payment = new Models.Entities.Payment
                {
                    PaymentId = paymentId,
                    UserId = request.UserId,
                    Amount = finalAmountWithGst, // Store the final amount with GST
                    Currency = request.Currency,
                    Purpose = request.Purpose,
                    Description = request.Description,
                    Status = "CREATED",
                    PaymentMethod = "razorpay",
                    RazorpayOrderId = order["id"].ToString(),
                    Receipt = request.Receipt ?? paymentId,
                    ShopDataJson = request.ShopData != null ? JsonSerializer.Serialize(request.ShopData) : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30) // 30 minutes expiry
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Prepare response notes with GST breakdown
                var responseNotes = new Dictionary<string, string>();
                
                // Add existing notes if any
                if (request.Notes != null)
                {
                    foreach (var note in request.Notes)
                    {
                        responseNotes[note.Key] = note.Value;
                    }
                }
                
                // Always add GST breakdown to response
                responseNotes["original_amount"] = request.Amount.ToString("F2");
                responseNotes["base_amount"] = gstBreakdown.BaseAmount.ToString("F2");
                responseNotes["discount_applied"] = discountAmount.ToString("F2");
                responseNotes["gst_rate"] = gstBreakdown.GstRate.ToString("F1");
                responseNotes["gst_amount"] = gstBreakdown.GstAmount.ToString("F2");
                responseNotes["final_amount_with_gst"] = finalAmountWithGst.ToString("F2");
                
                if (!string.IsNullOrEmpty(appliedCouponCode))
                {
                    responseNotes["coupon_code"] = appliedCouponCode;
                }

                var response = new RazorpayOrderResponseDto
                {
                    PaymentId = paymentId,
                    RazorpayOrderId = order["id"].ToString()!,
                    Amount = finalAmountWithGst, // Return the final amount with GST
                    Currency = request.Currency,
                    Status = "CREATED",
                    Receipt = payment.Receipt!,
                    Purpose = request.Purpose,
                    CreatedAt = payment.CreatedAt,
                    ExpiresAt = payment.ExpiresAt,
                    Notes = responseNotes,
                    RazorpayConfig = new RazorpayConfigDto
                    {
                        KeyId = _keyId,
                        Name = _configuration["Razorpay:CompanyName"] ?? "STIBE",
                        Description = request.Description ?? "Payment for STIBE services",
                        Image = _configuration["Razorpay:CompanyLogo"] ?? "",
                        Theme = _configuration["Razorpay:Theme"] ?? "#3399cc"
                    }
                };

                _logger.LogInformation("Razorpay order created successfully: PaymentId={PaymentId}, OrderId={OrderId}", 
                    (object)paymentId, (object)(order["id"]?.ToString() ?? ""));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Razorpay order for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<PaymentVerificationResponseDto> VerifyPaymentAsync(VerifyRazorpayPaymentRequestDto request)
        {
            try
            {
                _logger.LogInformation("Verifying Razorpay payment: PaymentId={PaymentId}, RazorpayPaymentId={RazorpayPaymentId}", 
                    request.PaymentId, request.RazorpayPaymentId);

                // Find payment record - try by internal PaymentId first, then by RazorpayOrderId
                stibe.api.Models.Entities.Payment? payment = null;
                
                if (!string.IsNullOrEmpty(request.PaymentId))
                {
                    payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);
                }
                else
                {
                    payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.RazorpayOrderId == request.RazorpayOrderId);
                }

                if (payment == null)
                {
                    throw new ArgumentException("Payment not found");
                }

                if (payment.Status == "CAPTURED")
                {
                    // Payment already verified
                    var existingShop = await _context.Shops
                        .FirstOrDefaultAsync(s => s.Id == payment.CreatedShopId);

                    return new PaymentVerificationResponseDto
                    {
                        PaymentId = payment.PaymentId,
                        RazorpayOrderId = payment.RazorpayOrderId!,
                        RazorpayPaymentId = payment.RazorpayPaymentId!,
                        Status = "SUCCESS",
                        Amount = payment.Amount,
                        Currency = payment.Currency,
                        PaymentMethod = payment.MethodType ?? "",
                        CompletedAt = payment.CompletedAt,
                        ShopData = existingShop != null ? MapShopToResponse(existingShop) : null
                    };
                }

                // Verify signature
                if (!VerifyRazorpaySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature))
                {
                    payment.Status = "FAILED";
                    payment.FailureReason = "Invalid signature";
                    payment.ErrorCode = "SIGNATURE_VERIFICATION_FAILED";
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    throw new UnauthorizedAccessException("Invalid payment signature");
                }

                // Fetch payment details from Razorpay
                var razorpayPayment = _razorpayClient.Payment.Fetch(request.RazorpayPaymentId);
                
                // Update payment record
                payment.RazorpayPaymentId = request.RazorpayPaymentId;
                payment.RazorpaySignature = request.RazorpaySignature;
                payment.Status = razorpayPayment["status"].ToString() == "captured" ? "CAPTURED" : "FAILED";
                payment.MethodType = razorpayPayment["method"]?.ToString();
                payment.Bank = razorpayPayment["bank"]?.ToString();
                payment.Wallet = razorpayPayment["wallet"]?.ToString();
                payment.VPA = razorpayPayment["vpa"]?.ToString();
                payment.RazorpayResponseJson = JsonSerializer.Serialize(razorpayPayment);
                payment.UpdatedAt = DateTime.UtcNow;

                if (payment.Status == "CAPTURED")
                {
                    payment.CompletedAt = DateTime.UtcNow;
                    
                    // Create shop if this is a shop registration payment
                    if (payment.Purpose == "SHOP_REGISTRATION" && !string.IsNullOrEmpty(payment.ShopDataJson))
                    {
                        var shopData = JsonSerializer.Deserialize<CreateShopPaymentDataDto>(payment.ShopDataJson);
                        if (shopData != null)
                        {
                            var createdShop = await CreateShopFromPaymentDataAsync(shopData, payment.UserId);
                            payment.CreatedShopId = createdShop.Id;
                        }
                    }
                    
                    // Mark coupon as used if payment included a coupon
                    // Extract coupon code from Razorpay order notes
                    var razorpayOrder = _razorpayClient.Order.Fetch(payment.RazorpayOrderId);
                    var orderNotes = razorpayOrder["notes"] as Newtonsoft.Json.Linq.JObject;
                    var couponCode = orderNotes?["coupon_code"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(couponCode))
                    {
                        try
                        {
                            await _couponService.MarkCouponAsUsedAsync(couponCode, payment.UserId, payment.RazorpayPaymentId);
                            _logger.LogInformation("Coupon marked as used: {CouponCode} for payment: {PaymentId}", 
                                couponCode, payment.PaymentId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to mark coupon as used: {CouponCode} for payment: {PaymentId}", 
                                couponCode, payment.PaymentId);
                            // Don't fail the payment verification if coupon marking fails
                        }
                    }

                    // Generate PDF receipt and send email for successful payment
                    await GenerateAndSendPaymentReceiptAsync(payment, couponCode);
                }
                else
                {
                    payment.FailureReason = razorpayPayment["error_description"]?.ToString();
                    payment.ErrorCode = razorpayPayment["error_code"]?.ToString();
                }

                await _context.SaveChangesAsync();

                var response = new PaymentVerificationResponseDto
                {
                    PaymentId = payment.PaymentId,
                    RazorpayOrderId = payment.RazorpayOrderId!,
                    RazorpayPaymentId = payment.RazorpayPaymentId!,
                    Status = payment.Status == "CAPTURED" ? "SUCCESS" : "FAILED",
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    PaymentMethod = payment.MethodType ?? "",
                    CompletedAt = payment.CompletedAt,
                    FailureReason = payment.FailureReason,
                    ErrorCode = payment.ErrorCode
                };

                if (payment.Status == "CAPTURED" && payment.CreatedShopId.HasValue)
                {
                    var shop = await _context.Shops.FindAsync(payment.CreatedShopId.Value);
                    if (shop != null)
                    {
                        response.ShopData = MapShopToResponse(shop);
                    }
                }

                _logger.LogInformation("Payment verification completed: PaymentId={PaymentId}, Status={Status}", 
                    payment.PaymentId, payment.Status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment: PaymentId={PaymentId}", request.PaymentId);
                throw;
            }
        }

        public async Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string paymentId)
        {
            try
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null)
                {
                    throw new ArgumentException("Payment not found");
                }

                // Check if payment has expired
                if (DateTime.UtcNow > payment.ExpiresAt && payment.Status == "CREATED")
                {
                    payment.Status = "EXPIRED";
                    payment.FailureReason = "Payment expired";
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return new PaymentStatusResponseDto
                {
                    PaymentId = payment.PaymentId,
                    RazorpayOrderId = payment.RazorpayOrderId ?? "",
                    RazorpayPaymentId = payment.RazorpayPaymentId,
                    Status = payment.Status,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Purpose = payment.Purpose,
                    PaymentMethod = payment.MethodType,
                    Bank = payment.Bank,
                    Wallet = payment.Wallet,
                    VPA = payment.VPA,
                    CreatedAt = payment.CreatedAt,
                    CompletedAt = payment.CompletedAt,
                    ExpiresAt = payment.ExpiresAt,
                    FailureReason = payment.FailureReason,
                    ErrorCode = payment.ErrorCode,
                    RefundedAmount = payment.RefundedAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for PaymentId={PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<RefundResponseDto> CreateRefundAsync(RefundRequestDto request)
        {
            try
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);

                if (payment == null)
                {
                    throw new ArgumentException("Payment not found");
                }

                if (payment.Status != "CAPTURED")
                {
                    throw new InvalidOperationException("Cannot refund uncaptured payment");
                }

                if (request.Amount > (payment.Amount - payment.RefundedAmount))
                {
                    throw new ArgumentException("Refund amount exceeds refundable amount");
                }

                // Create refund in Razorpay
                var refundRequest = new Dictionary<string, object>
                {
                    { "amount", (int)(request.Amount * 100) }, // Convert to paisa
                    { "receipt", request.Receipt ?? $"refund_{DateTime.UtcNow:yyyyMMddHHmmss}" },
                    { "notes", request.Notes }
                };

                if (!string.IsNullOrEmpty(request.Reason))
                {
                    var notes = (Dictionary<string, string>)refundRequest["notes"];
                    notes["reason"] = request.Reason;
                }

                var refund = _razorpayClient.Payment.Fetch(payment.RazorpayPaymentId!).Refund(refundRequest);

                // Update payment record
                payment.RefundedAmount += request.Amount;
                payment.RefundId = refund["id"].ToString();
                payment.RefundedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                if (payment.RefundedAmount >= payment.Amount)
                {
                    payment.Status = "REFUNDED";
                }

                await _context.SaveChangesAsync();

                return new RefundResponseDto
                {
                    RefundId = refund["id"].ToString()!,
                    PaymentId = payment.PaymentId,
                    RazorpayPaymentId = payment.RazorpayPaymentId!,
                    Amount = request.Amount,
                    Currency = payment.Currency,
                    Status = refund["status"].ToString()!,
                    Receipt = request.Receipt,
                    CreatedAt = DateTime.UtcNow,
                    Reason = request.Reason,
                    Notes = request.Notes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund for PaymentId={PaymentId}", request.PaymentId);
                throw;
            }
        }

        public async Task<bool> ProcessWebhookAsync(RazorpayWebhookDto webhookData, string signature)
        {
            try
            {
                _logger.LogInformation("Processing Razorpay webhook: Event={Event}", webhookData.Event);

                if (webhookData.Event == "payment.captured")
                {
                    var paymentEntity = webhookData.Payload.Payment;
                    var orderEntity = webhookData.Payload.Order;

                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.RazorpayOrderId == orderEntity.Id);

                    if (payment != null && payment.Status != "CAPTURED")
                    {
                        payment.RazorpayPaymentId = paymentEntity.Id;
                        payment.Status = "CAPTURED";
                        payment.MethodType = paymentEntity.Method;
                        payment.Bank = paymentEntity.Bank;
                        payment.Wallet = paymentEntity.Wallet;
                        payment.VPA = paymentEntity.Vpa;
                        payment.CompletedAt = DateTime.UtcNow;
                        payment.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Payment updated via webhook: PaymentId={PaymentId}", payment.PaymentId);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return false;
            }
        }

        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(_webhookSecret))
                {
                    _logger.LogWarning("Webhook signature verification skipped - no webhook secret configured");
                    return true; // Skip verification if no secret configured
                }

                var expectedSignature = ComputeHmacSha256(_webhookSecret, payload);
                return expectedSignature == signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook signature");
                return false;
            }
        }

        public PaymentConfigDto GetPaymentConfig()
        {
            return new PaymentConfigDto
            {
                ShopRegistrationFee = _configuration.GetValue<decimal>("Payment:ShopRegistrationFee", 500.0m),
                Currency = _configuration.GetValue<string>("Payment:Currency", "INR") ?? "INR",
                CurrencySymbol = "₹",
                PaymentTimeoutMinutes = _configuration.GetValue<int>("Payment:PaymentTimeoutMinutes", 30),
                MaxRetryAttempts = _configuration.GetValue<int>("Payment:MaxRetryAttempts", 3),
                SupportedPaymentMethods = _configuration.GetSection("Payment:SupportedMethods").Get<List<string>>() ?? 
                    new List<string> { "card", "netbanking", "wallet", "upi", "emi" },
                RazorpayConfig = new RazorpayConfigDto
                {
                    KeyId = _keyId,
                    Name = _configuration["Razorpay:CompanyName"] ?? "STIBE",
                    Theme = _configuration["Razorpay:Theme"] ?? "#3399cc",
                    Currency = _configuration.GetValue<string>("Payment:Currency", "INR") ?? "INR",
                    Modal = true
                }
            };
        }

        // Private helper methods
        private bool VerifyRazorpaySignature(string orderId, string paymentId, string signature)
        {
            var payload = $"{orderId}|{paymentId}";
            var expectedSignature = ComputeHmacSha256(_keySecret, payload);
            return expectedSignature == signature;
        }

        private string ComputeHmacSha256(string secret, string message)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private async Task<Shop> CreateShopFromPaymentDataAsync(CreateShopPaymentDataDto shopData, int ownerId)
        {
            // Validate time format
            if (!TimeSpan.TryParse(shopData.OpeningTime, out var openingTime))
            {
                throw new ArgumentException("Invalid opening time format");
            }

            if (!TimeSpan.TryParse(shopData.ClosingTime, out var closingTime))
            {
                throw new ArgumentException("Invalid closing time format");
            }

            // Check if user already has shops to determine if this should be default
            var existingShops = await _context.Shops
                .Where(s => s.OwnerId == ownerId && !s.IsDeleted)
                .CountAsync();
            
            bool isDefault = existingShops == 0;

            var shop = new Shop
            {
                Name = shopData.Name,
                Description = shopData.Description,
                Address = shopData.Address,
                City = shopData.City,
                State = shopData.State,
                ZipCode = shopData.ZipCode,
                PhoneNumber = shopData.PhoneNumber,
                Email = shopData.Email,
                ServiceType = shopData.ServiceType,
                GenderServices = shopData.GenderServices != null && shopData.GenderServices.Any() 
                    ? JsonSerializer.Serialize(shopData.GenderServices) 
                    : null,
                Specializations = shopData.Specializations != null && shopData.Specializations.Any() 
                    ? JsonSerializer.Serialize(shopData.Specializations) 
                    : null,
                BankAccountNumber = shopData.BankAccountNumber,
                IFSCCode = shopData.IFSCCode,
                BankName = shopData.BankName,
                AccountHolderName = shopData.AccountHolderName,
                GSTNumber = shopData.GSTNumber,
                PANNumber = shopData.PANNumber,
                OpeningTime = openingTime,
                ClosingTime = closingTime,
                BusinessHours = shopData.BusinessHours != null ? JsonSerializer.Serialize(shopData.BusinessHours) : null,
                Latitude = shopData.CurrentLatitude,
                Longitude = shopData.CurrentLongitude,
                ProfilePictureUrl = shopData.ProfilePictureUrl,
                ImageUrls = shopData.ImageUrls != null && shopData.ImageUrls.Any() 
                    ? JsonSerializer.Serialize(shopData.ImageUrls) 
                    : null,
                OwnerId = ownerId,
                IsActive = true,
                IsDefault = isDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();

            return shop;
        }

        private object MapShopToResponse(Shop shop)
        {
            return new
            {
                Id = shop.Id,
                Name = shop.Name ?? "",
                Description = shop.Description ?? "",
                Address = shop.Address ?? "",
                City = shop.City ?? "",
                State = shop.State ?? "",
                ZipCode = shop.ZipCode ?? "",
                PhoneNumber = shop.PhoneNumber ?? "",
                Email = shop.Email ?? "",
                ServiceType = shop.ServiceType,
                GenderServices = !string.IsNullOrEmpty(shop.GenderServices) 
                    ? JsonSerializer.Deserialize<List<string>>(shop.GenderServices) ?? new List<string>()
                    : new List<string>(),
                Specializations = !string.IsNullOrEmpty(shop.Specializations) 
                    ? JsonSerializer.Deserialize<List<string>>(shop.Specializations) ?? new List<string>()
                    : new List<string>(),
                BankAccountNumber = shop.BankAccountNumber,
                IFSCCode = shop.IFSCCode,
                BankName = shop.BankName,
                AccountHolderName = shop.AccountHolderName,
                GSTNumber = shop.GSTNumber,
                PANNumber = shop.PANNumber,
                OpeningTime = shop.OpeningTime,
                ClosingTime = shop.ClosingTime,
                BusinessHours = shop.BusinessHours,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                IsActive = shop.IsActive,
                IsDefault = shop.IsDefault,
                OwnerId = shop.OwnerId,
                CreatedAt = shop.CreatedAt,
                UpdatedAt = shop.UpdatedAt,
                ProfilePictureUrl = shop.ProfilePictureUrl ?? "",
                ImageUrls = !string.IsNullOrEmpty(shop.ImageUrls) 
                    ? JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>()
                    : new List<string>()
            };
        }

        private async Task GenerateAndSendPaymentReceiptAsync(stibe.api.Models.Entities.Payment payment, string? couponCode = null)
        {
            try
            {
                _logger.LogInformation("Generating payment receipt for PaymentId: {PaymentId}", payment.PaymentId);

                // Get user information
                var user = await _context.Users.FindAsync(payment.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for PaymentId: {PaymentId}, UserId: {UserId}", payment.PaymentId, payment.UserId);
                    return;
                }

                // Get shop information if this is a shop registration
                Shop? shop = null;
                if (payment.CreatedShopId.HasValue)
                {
                    shop = await _context.Shops.FindAsync(payment.CreatedShopId.Value);
                }

                // Get coupon information if coupon was applied
                decimal originalAmount = payment.Amount;
                decimal savings = 0;
                decimal discountPercentage = 0;
                string? couponDescription = null;

                if (!string.IsNullOrEmpty(couponCode))
                {
                    // Try to get coupon details for display
                    try
                    {
                        var validationRequest = new ValidateCouponRequestDto
                        {
                            CouponCode = couponCode,
                            Purpose = payment.Purpose,
                            OriginalAmount = payment.Amount,
                            UserEmail = user.Email,
                            PhoneNumber = user.PhoneNumber
                        };
                        
                        var couponValidation = await _couponService.ValidateCouponAsync(validationRequest);
                        if (couponValidation.IsValid)
                        {
                            originalAmount = couponValidation.OriginalAmount;
                            savings = couponValidation.Savings;
                            discountPercentage = couponValidation.DiscountPercentage;
                            couponDescription = couponValidation.Description;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve coupon details for receipt: {CouponCode}", couponCode);
                    }
                }

                // Calculate GST breakdown for receipt
                var baseAmountForGst = originalAmount - savings; // Amount after discount, before GST
                var gstBreakdown = _gstService.GetPaymentGstBreakdown(originalAmount, savings, couponCode);

                // Create receipt data
                var receiptData = new PaymentReceiptData
                {
                    PaymentId = payment.PaymentId,
                    RazorpayPaymentId = payment.RazorpayPaymentId ?? "",
                    RazorpayOrderId = payment.RazorpayOrderId ?? "",
                    Amount = payment.Amount, // Final amount including GST
                    BaseAmount = gstBreakdown.BaseAmount, // Amount before GST
                    GstRate = gstBreakdown.GstRate,
                    GstAmount = gstBreakdown.GstAmount,
                    CompanyGstNumber = gstBreakdown.CompanyGstNumber,
                    CustomerGstNumber = shop?.GSTNumber, // Get customer GST from shop data
                    OriginalAmount = originalAmount,
                    Savings = savings,
                    Currency = payment.Currency,
                    PaymentMethod = payment.MethodType ?? "Online",
                    CompletedAt = payment.CompletedAt ?? DateTime.UtcNow,
                    Purpose = payment.Purpose,
                    
                    // Customer Info
                    CustomerName = $"{user.FirstName} {user.LastName}".Trim(),
                    CustomerEmail = user.Email ?? "",
                    CustomerPhone = user.PhoneNumber ?? "",
                    
                    // Shop Info (if applicable)
                    ShopName = shop?.Name,
                    ShopAddress = shop?.Address,
                    ShopCity = shop?.City,
                    ShopState = shop?.State,
                    ShopZipCode = shop?.ZipCode,
                    
                    // Coupon Info (if applicable)
                    CouponCode = couponCode,
                    CouponDescription = couponDescription,
                    DiscountPercentage = discountPercentage
                };

                // Generate PDF
                var pdfBytes = await _pdfService.GeneratePaymentReceiptAsync(receiptData);
                var receiptFileName = $"Stibe_Receipt_{payment.PaymentId}_{DateTime.Now:yyyyMMdd}.pdf";

                // Send email with PDF attachment
                var emailSent = await _emailService.SendPaymentReceiptEmailAsync(
                    receiptData.CustomerEmail,
                    receiptData.CustomerName,
                    pdfBytes,
                    receiptFileName
                );

                if (emailSent)
                {
                    _logger.LogInformation("Payment receipt sent successfully to {Email} for PaymentId: {PaymentId}", 
                        receiptData.CustomerEmail, payment.PaymentId);
                }
                else
                {
                    _logger.LogWarning("Failed to send payment receipt email to {Email} for PaymentId: {PaymentId}", 
                        receiptData.CustomerEmail, payment.PaymentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and sending payment receipt for PaymentId: {PaymentId}", payment.PaymentId);
                // Don't throw - we don't want to fail the payment verification if receipt generation fails
            }
        }
    }
}
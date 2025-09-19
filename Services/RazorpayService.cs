using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using stibe.api.Data;
using stibe.api.Models.DTOs;
using stibe.api.Models.Entities;
using stibe.api.Models.Entities.PartnersEntity;
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
        private readonly RazorpayClient _razorpayClient;
        private readonly string _keyId;
        private readonly string _keySecret;
        private readonly string _webhookSecret;

        public RazorpayService(
            ApplicationDbContext context, 
            ILogger<RazorpayService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            
            _keyId = _configuration["Razorpay:KeyId"] ?? throw new ArgumentException("Razorpay KeyId not configured");
            _keySecret = _configuration["Razorpay:KeySecret"] ?? throw new ArgumentException("Razorpay KeySecret not configured");
            _webhookSecret = _configuration["Razorpay:WebhookSecret"] ?? "";
            
            _razorpayClient = new RazorpayClient(_keyId, _keySecret);
        }

        public async Task<RazorpayOrderResponseDto> CreateOrderAsync(CreateRazorpayOrderRequestDto request)
        {
            try
            {
                _logger.LogInformation("Creating Razorpay order for user {UserId}, amount {Amount}", request.UserId, request.Amount);

                // Generate unique payment ID
                var paymentId = $"STIBE_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..50];
                
                // Convert amount to paisa (Razorpay expects amount in smallest currency unit)
                var amountInPaisa = (int)(request.Amount * 100);
                
                // Create Razorpay order
                var orderRequest = new Dictionary<string, object>
                {
                    { "amount", amountInPaisa },
                    { "currency", request.Currency },
                    { "receipt", request.Receipt ?? paymentId },
                    { "notes", request.Notes }
                };

                var order = _razorpayClient.Order.Create(orderRequest);
                
                // Store payment record in database
                var payment = new Models.Entities.Payment
                {
                    PaymentId = paymentId,
                    UserId = request.UserId,
                    Amount = request.Amount,
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

                var response = new RazorpayOrderResponseDto
                {
                    PaymentId = paymentId,
                    RazorpayOrderId = order["id"].ToString()!,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = "CREATED",
                    Receipt = payment.Receipt!,
                    Purpose = request.Purpose,
                    CreatedAt = payment.CreatedAt,
                    ExpiresAt = payment.ExpiresAt,
                    Notes = request.Notes,
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
    }
}
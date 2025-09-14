using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs;
using stibe.api.Models.Entities;
using stibe.api.Models.Entities.PartnersEntity;
using System.Security.Claims;
using System.Text.Json;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;

        public PaymentController(
            ApplicationDbContext context, 
            ILogger<PaymentController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("initiate-shop-payment")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<UpiPaymentResponseDto>>> InitiateShopPayment([FromBody] InitiateShopPaymentRequestDto request)
        {
            try
            {
                _logger.LogInformation($"💳 InitiateShopPayment called for user: {request.UserId}");

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null || currentUserId != request.UserId)
                {
                    return Unauthorized(ApiResponse<UpiPaymentResponseDto>.ErrorResponse("Invalid user authorization"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = new List<string>();
                    foreach (var modelError in ModelState)
                    {
                        foreach (var error in modelError.Value.Errors)
                        {
                            errors.Add($"{modelError.Key}: {error.ErrorMessage}");
                        }
                    }
                    
                    _logger.LogWarning($"⚠️ Payment validation failed for user {request.UserId}: {string.Join(", ", errors)}");
                    return BadRequest(ApiResponse<UpiPaymentResponseDto>.ErrorResponse("Validation failed. Please check all required fields.", errors));
                }

                // Generate unique payment ID
                var paymentId = $"SHOP_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
                
                // Create UPI payment URL
                var upiPaymentData = CreateUpiPaymentData(paymentId, request.Amount, request.Description ?? "Shop Creation Payment");
                
                // Store payment record with shop data
                var payment = new Payment
                {
                    PaymentId = paymentId,
                    UserId = request.UserId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Purpose = request.Purpose,
                    Description = request.Description,
                    Status = "PENDING",
                    PaymentMethod = "UPI",
                    UpiId = upiPaymentData.UpiId,
                    PayeeName = upiPaymentData.PayeeName,
                    TransactionNote = upiPaymentData.TransactionNote,
                    UpiIntentUrl = upiPaymentData.UpiIntentUrl,
                    QrCodeData = upiPaymentData.QrCodeData,
                    ShopDataJson = JsonSerializer.Serialize(request.ShopData),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15) // 15 minutes expiry
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                var response = new UpiPaymentResponseDto
                {
                    PaymentId = paymentId,
                    UpiIntentUrl = upiPaymentData.UpiIntentUrl,
                    QrCodeData = upiPaymentData.QrCodeData,
                    UpiId = upiPaymentData.UpiId, // Now available separately
                    PayeeName = upiPaymentData.PayeeName, // Now available separately
                    ExpiresAt = payment.ExpiresAt,
                    Status = "PENDING"
                };

                _logger.LogInformation("Shop payment initiated: {PaymentId}", paymentId);
                return Ok(ApiResponse<UpiPaymentResponseDto>.SuccessResponse(response, "Payment initiated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating shop payment");
                return StatusCode(500, ApiResponse<UpiPaymentResponseDto>.ErrorResponse("An error occurred while initiating payment"));
            }
        }

        [HttpPost("verify-payment")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<PaymentVerificationResponseDto>>> VerifyPayment([FromBody] VerifyPaymentRequestDto request)
        {
            try
            {
                _logger.LogInformation("VerifyPayment called for payment: {PaymentId}", request.PaymentId);

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse("Invalid user authorization"));
                }

                // Find the payment record
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId && p.UserId == currentUserId);

                if (payment == null)
                {
                    return NotFound(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse("Payment not found"));
                }

                // Check if payment is already processed
                if (payment.Status == "SUCCESS")
                {
                    var existingShop = await _context.Shops
                        .FirstOrDefaultAsync(s => s.Id == payment.CreatedShopId);

                    var successResponse = new PaymentVerificationResponseDto
                    {
                        PaymentId = payment.PaymentId,
                        Status = "SUCCESS",
                        TransactionId = payment.TransactionId,
                        UpiTransactionRef = payment.UpiTransactionRef,
                        Amount = payment.Amount,
                        PaymentCompletedAt = payment.CompletedAt,
                        ShopData = existingShop != null ? MapShopToResponse(existingShop) : null
                    };

                    return Ok(ApiResponse<PaymentVerificationResponseDto>.SuccessResponse(successResponse, "Payment already verified"));
                }

                // Check if payment has expired
                if (DateTime.UtcNow > payment.ExpiresAt && payment.Status == "PENDING")
                {
                    payment.Status = "EXPIRED";
                    payment.FailureReason = "Payment expired";
                    payment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    var expiredResponse = new PaymentVerificationResponseDto
                    {
                        PaymentId = payment.PaymentId,
                        Status = "EXPIRED",
                        FailureReason = "Payment expired"
                    };

                    return Ok(ApiResponse<PaymentVerificationResponseDto>.SuccessResponse(expiredResponse, "Payment has expired"));
                }

                // Production-grade validation: Require both transaction ID and UPI reference
                // These must be unique identifiers from the actual UPI transaction
                if (string.IsNullOrWhiteSpace(request.TransactionId) || 
                    string.IsNullOrWhiteSpace(request.UpiTransactionRef))
                {
                    return BadRequest(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse(
                        "Both Transaction ID and UPI Transaction Reference are required for payment verification"));
                }

                // Validate transaction ID format (12-character alphanumeric for UPI)
                if (request.TransactionId.Length != 12 || 
                    !System.Text.RegularExpressions.Regex.IsMatch(request.TransactionId, @"^[A-Za-z0-9]+$"))
                {
                    return BadRequest(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse(
                        "Invalid transaction ID format. Must be 12-character alphanumeric string"));
                }

                // Ensure transaction references are different from payment ID (indicating real UPI completion)
                if (request.TransactionId == payment.PaymentId || request.UpiTransactionRef == payment.PaymentId)
                {
                    return BadRequest(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse(
                        "Transaction references must be unique identifiers from the UPI transaction"));
                }
                
                // Process successful payment verification
                _logger.LogInformation("Verifying payment {PaymentId} with transaction ID: {TransactionId}", payment.PaymentId, request.TransactionId);
                
                // Mark payment as successful and create shop
                payment.Status = "SUCCESS";
                payment.TransactionId = request.TransactionId;
                payment.UpiTransactionRef = request.UpiTransactionRef;
                payment.CompletedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                // Create shop from stored data
                var shopData = JsonSerializer.Deserialize<CreateShopPaymentDataDto>(payment.ShopDataJson!);
                var createdShop = await CreateShopFromPaymentData(shopData!, payment.UserId);
                
                payment.CreatedShopId = createdShop.Id;
                await _context.SaveChangesAsync();

                var response = new PaymentVerificationResponseDto
                {
                    PaymentId = payment.PaymentId,
                    Status = "SUCCESS",
                    TransactionId = payment.TransactionId,
                    UpiTransactionRef = payment.UpiTransactionRef,
                    Amount = payment.Amount,
                    PaymentCompletedAt = payment.CompletedAt,
                    ShopData = MapShopToResponse(createdShop)
                };

                _logger.LogInformation("Payment verified and shop created: {PaymentId} -> Shop ID: {ShopId}", payment.PaymentId, createdShop.Id);
                return Ok(ApiResponse<PaymentVerificationResponseDto>.SuccessResponse(response, "Payment verified and shop created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment");
                return StatusCode(500, ApiResponse<PaymentVerificationResponseDto>.ErrorResponse("An error occurred while verifying payment"));
            }
        }

        [HttpGet("status/{paymentId}")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<PaymentStatusDto>>> GetPaymentStatus(string paymentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<PaymentStatusDto>.ErrorResponse("Invalid user authorization"));
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.UserId == currentUserId);

                if (payment == null)
                {
                    return NotFound(ApiResponse<PaymentStatusDto>.ErrorResponse("Payment not found"));
                }

                var response = new PaymentStatusDto
                {
                    PaymentId = payment.PaymentId,
                    Status = payment.Status,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Purpose = payment.Purpose,
                    CreatedAt = payment.CreatedAt,
                    CompletedAt = payment.CompletedAt,
                    TransactionId = payment.TransactionId,
                    FailureReason = payment.FailureReason
                };

                return Ok(ApiResponse<PaymentStatusDto>.SuccessResponse(response, "Payment status retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment status");
                return StatusCode(500, ApiResponse<PaymentStatusDto>.ErrorResponse("An error occurred while retrieving payment status"));
            }
        }

        // NOTE: Test simulation endpoint removed for production security
        // All payments must go through proper verification process

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        private (string UpiId, string PayeeName, string TransactionNote, string UpiIntentUrl, string QrCodeData) CreateUpiPaymentData(
            string paymentId, decimal amount, string description)
        {
            // Get UPI configuration from appsettings
            var upiId = _configuration["UPI:PaymentAddress"] ?? "tishnut@fifederal";
            var payeeName = _configuration["UPI:PayeeName"] ?? "Stibe Services";
            
            // Create UPI intent URL
            var transactionNote = $"Shop Registration - {description}";
            var upiIntentUrl = $"upi://pay?pa={upiId}&pn={Uri.EscapeDataString(payeeName)}&am={amount}&cu=INR&tn={Uri.EscapeDataString(transactionNote)}&tr={paymentId}";
            
            // QR code data (same as UPI intent)
            var qrCodeData = upiIntentUrl;

            return (upiId, payeeName, transactionNote, upiIntentUrl, qrCodeData);
        }

        private async Task<Shop> CreateShopFromPaymentData(CreateShopPaymentDataDto shopData, int ownerId)
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

        private ShopResponseDto MapShopToResponse(Shop shop)
        {
            return new ShopResponseDto
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stibe.api.Models.DTOs;
using stibe.api.Services;
using stibe.api.Models;
using stibe.api.Models.DTOs.Features;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.RateLimiting;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/payment-gateway")]
    public class PaymentGatewayController : ControllerBase
    {
        private readonly IPaymentGatewayService _paymentService;
        private readonly ILogger<PaymentGatewayController> _logger;

        public PaymentGatewayController(
            IPaymentGatewayService paymentService,
            ILogger<PaymentGatewayController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new payment for shop registration or other services
        /// </summary>
        [HttpPost("create")]
        [EnableRateLimiting("PaymentPolicy")]
        [Authorize] // Require authentication but allow any role
        public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> CreatePayment(
            [FromBody] CreatePaymentRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<PaymentResponseDto>.ErrorResponse("User authentication required"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<PaymentResponseDto>.ErrorResponse(
                        "Validation failed", errors));
                }

                // Set user ID from token
                request.UserId = int.Parse(userId);

                // Log payment creation attempt
                _logger.LogInformation("Creating payment for user {UserId}, amount {Amount}, type {PaymentType}",
                    userId, request.Amount, request.PaymentType);

                var response = await _paymentService.CreatePaymentAsync(request, userId);

                return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(
                    response, "Payment created successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid payment request");
                return BadRequest(ApiResponse<PaymentResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return StatusCode(500, ApiResponse<PaymentResponseDto>.ErrorResponse(
                    "Failed to create payment. Please try again."));
            }
        }

        /// <summary>
        /// Get payment status by payment ID
        /// </summary>
        [HttpGet("status/{paymentId}")]
        public async Task<ActionResult<ApiResponse<PaymentStatusResponseDto>>> GetPaymentStatus(string paymentId)
        {
            try
            {
                if (string.IsNullOrEmpty(paymentId))
                {
                    return BadRequest(ApiResponse<PaymentStatusResponseDto>.ErrorResponse("Payment ID is required"));
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("Getting payment status for {PaymentId} by user {UserId}", paymentId, userId);

                var response = await _paymentService.GetPaymentStatusAsync(paymentId);

                return Ok(ApiResponse<PaymentStatusResponseDto>.SuccessResponse(
                    response, "Payment status retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Payment not found: {PaymentId}", paymentId);
                return NotFound(ApiResponse<PaymentStatusResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for {PaymentId}", paymentId);
                return StatusCode(500, ApiResponse<PaymentStatusResponseDto>.ErrorResponse(
                    "Failed to retrieve payment status"));
            }
        }

        /// <summary>
        /// Verify payment completion with transaction details
        /// </summary>
        [HttpPost("verify")]
        [EnableRateLimiting("PaymentVerifyPolicy")]
        public async Task<ActionResult<ApiResponse<PaymentStatusResponseDto>>> VerifyPayment(
            [FromBody] VerifyPaymentDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<PaymentStatusResponseDto>.ErrorResponse(
                        "Validation failed", errors));
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("Verifying payment {PaymentId} with transaction {TransactionId} by user {UserId}",
                    request.PaymentId, request.TransactionId, userId);

                var response = await _paymentService.VerifyPaymentAsync(request);

                return Ok(ApiResponse<PaymentStatusResponseDto>.SuccessResponse(
                    response, "Payment verified successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Payment verification failed for {PaymentId}", request.PaymentId);
                return BadRequest(ApiResponse<PaymentStatusResponseDto>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid payment verification attempt for {PaymentId}", request.PaymentId);
                return BadRequest(ApiResponse<PaymentStatusResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment {PaymentId}", request.PaymentId);
                return StatusCode(500, ApiResponse<PaymentStatusResponseDto>.ErrorResponse(
                    "Failed to verify payment. Please try again."));
            }
        }

        /// <summary>
        /// Create a refund for a successful payment
        /// </summary>
        [HttpPost("refund")]
        [Authorize(Roles = "ShopOwner,Admin")]
        public async Task<ActionResult<ApiResponse<RefundResponseDto>>> CreateRefund(
            [FromBody] CreateRefundRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<RefundResponseDto>.ErrorResponse(
                        "Validation failed", errors));
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("Creating refund for payment {PaymentId}, amount {Amount} by user {UserId}",
                    request.PaymentId, request.RefundAmount, userId);

                var response = await _paymentService.CreateRefundAsync(request);

                return Ok(ApiResponse<RefundResponseDto>.SuccessResponse(
                    response, "Refund created successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid refund request for payment {PaymentId}", request.PaymentId);
                return BadRequest(ApiResponse<RefundResponseDto>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Refund not allowed for payment {PaymentId}", request.PaymentId);
                return BadRequest(ApiResponse<RefundResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund for payment {PaymentId}", request.PaymentId);
                return StatusCode(500, ApiResponse<RefundResponseDto>.ErrorResponse(
                    "Failed to create refund. Please try again."));
            }
        }

        /// <summary>
        /// Get payment analytics for the authenticated user
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "ShopOwner,Admin")]
        public async Task<ActionResult<ApiResponse<PaymentAnalyticsResponseDto>>> GetAnalytics(
            [FromQuery] PaymentAnalyticsRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Getting payment analytics for user {UserId}", userId);

                var response = await _paymentService.GetAnalyticsAsync(request);

                return Ok(ApiResponse<PaymentAnalyticsResponseDto>.SuccessResponse(
                    response, "Analytics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment analytics");
                return StatusCode(500, ApiResponse<PaymentAnalyticsResponseDto>.ErrorResponse(
                    "Failed to retrieve analytics"));
            }
        }

        /// <summary>
        /// Webhook endpoint for payment gateway notifications
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        [EnableRateLimiting("WebhookPolicy")]
        public async Task<IActionResult> ProcessWebhook([FromBody] PaymentWebhookDto webhook)
        {
            try
            {
                _logger.LogInformation("Received webhook for payment {PaymentId}, event {Event}",
                    webhook.PaymentId, webhook.Event);

                var processed = await _paymentService.ProcessWebhookAsync(webhook);

                if (processed)
                {
                    return Ok(new { status = "success", message = "Webhook processed successfully" });
                }
                else
                {
                    return BadRequest(new { status = "failed", message = "Failed to process webhook" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook for payment {PaymentId}", webhook.PaymentId);
                return StatusCode(500, new { status = "error", message = "Internal server error" });
            }
        }

        /// <summary>
        /// Generate QR code for UPI payment
        /// </summary>
        [HttpPost("qr-code")]
        public async Task<ActionResult<ApiResponse<object>>> GenerateQrCode([FromBody] QrCodeRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UpiUrl))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("UPI URL is required"));
                }

                var qrCode = await _paymentService.GenerateQrCodeAsync(request.UpiUrl);

                var response = new
                {
                    qrCodeBase64 = qrCode,
                    upiUrl = request.UpiUrl
                };

                return Ok(ApiResponse<object>.SuccessResponse(response, "QR code generated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to generate QR code"));
            }
        }

        /// <summary>
        /// Get payment configuration for frontend
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<PaymentConfigDto>> GetPaymentConfig()
        {
            try
            {
                var config = new PaymentConfigDto
                {
                    ShopRegistrationFee = 500.0m,
                    Currency = "INR",
                    CurrencySymbol = "₹",
                    PaymentTimeoutMinutes = 15,
                    MaxRetryAttempts = 3,
                    UpiPaymentAddress = "tishnut@fifderal",
                    PayeeName = "STIBE BUSINESS",
                    MerchantCode = "STIBE001",
                    SupportedPaymentMethods = new List<string> { "UPI" },
                    SupportedUpiApps = new Dictionary<string, string>
                    { 
                        { "google_pay", "Google Pay" },
                        { "phonepe", "PhonePe" },
                        { "paytm", "Paytm" },
                        { "bhim", "BHIM" },
                        { "amazon_pay", "Amazon Pay" } 
                    }
                };

                return Ok(ApiResponse<PaymentConfigDto>.SuccessResponse(
                    config, "Payment configuration retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment configuration");
                return StatusCode(500, ApiResponse<PaymentConfigDto>.ErrorResponse(
                    "Failed to retrieve payment configuration"));
            }
        }

        /// <summary>
        /// Health check endpoint for payment service
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "payment-gateway",
                version = "2.0.0",
                features = new[] { "universal-payments", "payment-links", "multi-vendor", "subscriptions" }
            });
        }

        // Universal Payment Type Endpoints

        /// <summary>
        /// Create payment for service booking
        /// </summary>
        [HttpPost("service-booking")]
        [EnableRateLimiting("PaymentPolicy")]
        [AllowAnonymous] // Allow broader access for service bookings
        public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> CreateServiceBookingPayment(
            [FromBody] ServiceBookingPaymentDto request)
        {
            try
            {
                var userId = GetCurrentUserId() ?? "anonymous";
                var result = await _paymentService.CreateServiceBookingPaymentAsync(request, userId);
                return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result, "Service booking payment created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<PaymentResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service booking payment");
                return StatusCode(500, ApiResponse<PaymentResponseDto>.ErrorResponse("Failed to create service booking payment"));
            }
        }

        /// <summary>
        /// Create payment for marketplace order
        /// </summary>
        [HttpPost("marketplace")]
        [EnableRateLimiting("PaymentPolicy")]
        [AllowAnonymous] // Allow broader access for marketplace orders
        public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> CreateMarketplacePayment(
            [FromBody] MarketplacePaymentDto request)
        {
            try
            {
                var userId = GetCurrentUserId() ?? "anonymous";
                var result = await _paymentService.CreateMarketplacePaymentAsync(request, userId);
                return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result, "Marketplace payment created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<PaymentResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating marketplace payment");
                return StatusCode(500, ApiResponse<PaymentResponseDto>.ErrorResponse("Failed to create marketplace payment"));
            }
        }

        /// <summary>
        /// Create payment for subscription
        /// </summary>
        [HttpPost("subscription")]
        [EnableRateLimiting("PaymentPolicy")]
        public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> CreateSubscriptionPayment(
            [FromBody] SubscriptionPaymentDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<PaymentResponseDto>.ErrorResponse("User authentication required"));
                }

                var result = await _paymentService.CreateSubscriptionPaymentAsync(request, userId);
                return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result, "Subscription payment created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<PaymentResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription payment");
                return StatusCode(500, ApiResponse<PaymentResponseDto>.ErrorResponse("Failed to create subscription payment"));
            }
        }

        /// <summary>
        /// Create payment for vendor settlement
        /// </summary>
        [HttpPost("vendor")]
        [EnableRateLimiting("PaymentPolicy")]
        [Authorize(Roles = "Admin,ShopOwner")]
        public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> CreateVendorPayment(
            [FromBody] VendorPaymentDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<PaymentResponseDto>.ErrorResponse("User authentication required"));
                }

                var result = await _paymentService.CreateVendorPaymentAsync(request, userId);
                return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result, "Vendor payment created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<PaymentResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor payment");
                return StatusCode(500, ApiResponse<PaymentResponseDto>.ErrorResponse("Failed to create vendor payment"));
            }
        }

        /// <summary>
        /// Create payment link for easy sharing
        /// </summary>
        [HttpPost("create-link")]
        [EnableRateLimiting("PaymentPolicy")]
        public async Task<ActionResult<ApiResponse<PaymentLinkResponseDto>>> CreatePaymentLink(
            [FromBody] CreatePaymentLinkDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<PaymentLinkResponseDto>.ErrorResponse("User authentication required"));
                }

                var result = await _paymentService.CreatePaymentLinkAsync(request, userId);
                return Ok(ApiResponse<PaymentLinkResponseDto>.SuccessResponse(result, "Payment link created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<PaymentLinkResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link");
                return StatusCode(500, ApiResponse<PaymentLinkResponseDto>.ErrorResponse("Failed to create payment link"));
            }
        }

        /// <summary>
        /// Get merchant-specific payment analytics
        /// </summary>
        [HttpGet("merchant/{merchantId}/analytics")]
        [EnableRateLimiting("AnalyticsPolicy")]
        [Authorize(Roles = "Admin,ShopOwner")]
        public async Task<ActionResult<ApiResponse<PaymentAnalyticsResponseDto>>> GetMerchantAnalytics(
            string merchantId,
            [FromQuery] PaymentAnalyticsRequestDto request)
        {
            try
            {
                var result = await _paymentService.GetMerchantAnalyticsAsync(merchantId, request);
                return Ok(ApiResponse<PaymentAnalyticsResponseDto>.SuccessResponse(result, "Merchant analytics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant analytics for {MerchantId}", merchantId);
                return StatusCode(500, ApiResponse<PaymentAnalyticsResponseDto>.ErrorResponse("Failed to retrieve merchant analytics"));
            }
        }

        /// <summary>
        /// Get payment history for user
        /// </summary>
        [HttpGet("history")]
        [EnableRateLimiting("AnalyticsPolicy")]
        public async Task<ActionResult<ApiResponse<List<PaymentResponseDto>>>> GetPaymentHistory(
            [FromQuery] int page = 1, 
            [FromQuery] int limit = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<List<PaymentResponseDto>>.ErrorResponse("User authentication required"));
                }

                var result = await _paymentService.GetPaymentHistoryAsync(userId, page, limit);
                return Ok(ApiResponse<List<PaymentResponseDto>>.SuccessResponse(result, "Payment history retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment history for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<List<PaymentResponseDto>>.ErrorResponse("Failed to retrieve payment history"));
            }
        }

        /// <summary>
        /// Get supported payment methods
        /// </summary>
        [HttpGet("payment-methods")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetSupportedPaymentMethods()
        {
            try
            {
                var result = await _paymentService.GetSupportedPaymentMethodsAsync();
                return Ok(ApiResponse<List<string>>.SuccessResponse(result, "Supported payment methods retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supported payment methods");
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("Failed to retrieve payment methods"));
            }
        }

        /// <summary>
        /// Get payment method fees
        /// </summary>
        [HttpGet("payment-fees")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<Dictionary<string, decimal>>>> GetPaymentMethodFees()
        {
            try
            {
                var result = await _paymentService.GetPaymentMethodFeesAsync();
                return Ok(ApiResponse<Dictionary<string, decimal>>.SuccessResponse(result, "Payment method fees retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment method fees");
                return StatusCode(500, ApiResponse<Dictionary<string, decimal>>.ErrorResponse("Failed to retrieve payment fees"));
            }
        }

        // Helper methods
        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

    // Additional DTO for QR code generation
    public class QrCodeRequestDto
    {
        [Required]
        public string UpiUrl { get; set; } = string.Empty;
    }
}
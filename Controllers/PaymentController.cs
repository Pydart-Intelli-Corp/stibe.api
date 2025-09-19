using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stibe.api.Models.DTOs;
using stibe.api.Models.DTOs.Features;
using stibe.api.Services;
using System.Security.Claims;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IRazorpayService _razorpayService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IRazorpayService razorpayService,
            ILogger<PaymentController> logger)
        {
            _razorpayService = razorpayService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new Razorpay order for payment
        /// </summary>
        [HttpPost("create-order")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<RazorpayOrderResponseDto>>> CreateOrder([FromBody] CreateRazorpayOrderRequestDto request)
        {
            try
            {
                _logger.LogInformation("Creating Razorpay order for user: {UserId}", request.UserId);

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null || currentUserId != request.UserId)
                {
                    return Unauthorized(ApiResponse<RazorpayOrderResponseDto>.ErrorResponse("Invalid user authorization"));
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
                    
                    _logger.LogWarning("Order creation validation failed for user {UserId}: {Errors}", request.UserId, string.Join(", ", errors));
                    return BadRequest(ApiResponse<RazorpayOrderResponseDto>.ErrorResponse("Validation failed. Please check all required fields.", errors));
                }

                var response = await _razorpayService.CreateOrderAsync(request);

                _logger.LogInformation("Razorpay order created successfully: {PaymentId}", response.PaymentId);
                return Ok(ApiResponse<RazorpayOrderResponseDto>.SuccessResponse(response, "Order created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Razorpay order");
                return StatusCode(500, ApiResponse<RazorpayOrderResponseDto>.ErrorResponse("An error occurred while creating the order"));
            }
        }

        /// <summary>
        /// Initiate shop payment with Razorpay
        /// </summary>
        [HttpPost("initiate-shop-payment")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<RazorpayOrderResponseDto>>> InitiateShopPayment([FromBody] CreateRazorpayOrderRequestDto request)
        {
            try
            {
                _logger.LogInformation("Initiating shop payment for user: {UserId}", request.UserId);

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null || currentUserId != request.UserId)
                {
                    return Unauthorized(ApiResponse<RazorpayOrderResponseDto>.ErrorResponse("Invalid user authorization"));
                }

                // Ensure this is a shop registration payment
                request.Purpose = "SHOP_REGISTRATION";
                request.Description = request.Description ?? "Shop Registration Payment";

                if (request.ShopData == null)
                {
                    return BadRequest(ApiResponse<RazorpayOrderResponseDto>.ErrorResponse("Shop data is required for shop registration payment"));
                }

                var response = await _razorpayService.CreateOrderAsync(request);

                _logger.LogInformation("Shop payment initiated successfully: {PaymentId}", response.PaymentId);
                return Ok(ApiResponse<RazorpayOrderResponseDto>.SuccessResponse(response, "Shop payment initiated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating shop payment");
                return StatusCode(500, ApiResponse<RazorpayOrderResponseDto>.ErrorResponse("An error occurred while initiating shop payment"));
            }
        }

        /// <summary>
        /// Verify Razorpay payment after successful payment
        /// </summary>
        [HttpPost("verify-payment")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<PaymentVerificationResponseDto>>> VerifyPayment([FromBody] VerifyRazorpayPaymentRequestDto request)
        {
            try
            {
                _logger.LogInformation("Verifying payment: {PaymentId}", request.PaymentId);

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse("Invalid user authorization"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse("Validation failed", errors));
                }

                var response = await _razorpayService.VerifyPaymentAsync(request);

                _logger.LogInformation("Payment verification completed: {PaymentId}, Status: {Status}", request.PaymentId, response.Status);
                return Ok(ApiResponse<PaymentVerificationResponseDto>.SuccessResponse(response, "Payment verified successfully"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Payment verification failed: Invalid signature");
                return BadRequest(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse("Payment verification failed: Invalid signature"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Payment verification failed: {Message}", ex.Message);
                return NotFound(ApiResponse<PaymentVerificationResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment");
                return StatusCode(500, ApiResponse<PaymentVerificationResponseDto>.ErrorResponse("An error occurred while verifying payment"));
            }
        }

        /// <summary>
        /// Get payment status by payment ID
        /// </summary>
        [HttpGet("status/{paymentId}")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<PaymentStatusResponseDto>>> GetPaymentStatus(string paymentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<PaymentStatusResponseDto>.ErrorResponse("Invalid user authorization"));
                }

                var response = await _razorpayService.GetPaymentStatusAsync(paymentId);

                return Ok(ApiResponse<PaymentStatusResponseDto>.SuccessResponse(response, "Payment status retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Payment not found: {PaymentId}", paymentId);
                return NotFound(ApiResponse<PaymentStatusResponseDto>.ErrorResponse("Payment not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment status");
                return StatusCode(500, ApiResponse<PaymentStatusResponseDto>.ErrorResponse("An error occurred while retrieving payment status"));
            }
        }

        /// <summary>
        /// Create a refund for a successful payment
        /// </summary>
        [HttpPost("refund")]
        [Authorize(Roles = "ShopOwner,Admin")]
        public async Task<ActionResult<ApiResponse<RefundResponseDto>>> CreateRefund([FromBody] RefundRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<RefundResponseDto>.ErrorResponse("Invalid user authorization"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<RefundResponseDto>.ErrorResponse("Validation failed", errors));
                }

                var response = await _razorpayService.CreateRefundAsync(request);

                _logger.LogInformation("Refund created successfully: {RefundId} for payment {PaymentId}", response.RefundId, request.PaymentId);
                return Ok(ApiResponse<RefundResponseDto>.SuccessResponse(response, "Refund created successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Refund creation failed: {Message}", ex.Message);
                return BadRequest(ApiResponse<RefundResponseDto>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Refund not allowed: {Message}", ex.Message);
                return BadRequest(ApiResponse<RefundResponseDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund");
                return StatusCode(500, ApiResponse<RefundResponseDto>.ErrorResponse("An error occurred while creating refund"));
            }
        }

        /// <summary>
        /// Webhook endpoint for Razorpay notifications
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessWebhook([FromBody] RazorpayWebhookDto webhook, [FromHeader(Name = "X-Razorpay-Signature")] string signature)
        {
            try
            {
                _logger.LogInformation("Received Razorpay webhook: {Event}", webhook.Event);

                // Read the raw request body for signature verification
                Request.EnableBuffering();
                Request.Body.Position = 0;
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();

                // Verify webhook signature
                if (!_razorpayService.VerifyWebhookSignature(rawBody, signature))
                {
                    _logger.LogWarning("Webhook signature verification failed");
                    return Unauthorized(new { status = "failed", message = "Invalid signature" });
                }

                var processed = await _razorpayService.ProcessWebhookAsync(webhook, signature);

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
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, new { status = "error", message = "Internal server error" });
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
                var config = _razorpayService.GetPaymentConfig();
                return Ok(ApiResponse<PaymentConfigDto>.SuccessResponse(config, "Payment configuration retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment configuration");
                return StatusCode(500, ApiResponse<PaymentConfigDto>.ErrorResponse("An error occurred while retrieving payment configuration"));
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
                service = "razorpay-payment-gateway",
                version = "1.0.0",
                features = new[] { "razorpay-orders", "payment-verification", "refunds", "webhooks", "shop-registration" }
            });
        }

        // Helper methods
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}
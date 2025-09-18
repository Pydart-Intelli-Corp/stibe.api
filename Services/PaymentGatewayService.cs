using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models;
using stibe.api.Models.Entities;
using stibe.api.Models.DTOs;
using stibe.api.Models.Entities.PartnersEntity;
using QRCoder;
using PaymentStatus = stibe.api.Models.Entities.PaymentStatus;
using RefundStatus = stibe.api.Models.Entities.RefundStatus;

namespace stibe.api.Services
{
    public interface IPaymentGatewayService
    {
        // Universal Payment Creation
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request, string userId);
        Task<PaymentResponseDto> CreateServiceBookingPaymentAsync(ServiceBookingPaymentDto request, string userId);
        Task<PaymentResponseDto> CreateMarketplacePaymentAsync(MarketplacePaymentDto request, string userId);
        Task<PaymentResponseDto> CreateSubscriptionPaymentAsync(SubscriptionPaymentDto request, string userId);
        Task<PaymentResponseDto> CreateVendorPaymentAsync(VendorPaymentDto request, string userId);
        
        // Payment Links
        Task<PaymentLinkResponseDto> CreatePaymentLinkAsync(CreatePaymentLinkDto request, string userId);
        Task<PaymentResponseDto> PayUsingLinkAsync(string linkId, string customerId);
        
        // Standard Payment Operations
        Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string paymentId);
        Task<PaymentStatusResponseDto> VerifyPaymentAsync(VerifyPaymentDto request);
        Task<RefundResponseDto> CreateRefundAsync(CreateRefundRequestDto request);
        Task<bool> ProcessWebhookAsync(PaymentWebhookDto webhook);
        
        // Analytics & Reporting
        Task<PaymentAnalyticsResponseDto> GetAnalyticsAsync(PaymentAnalyticsRequestDto request);
        Task<PaymentAnalyticsResponseDto> GetMerchantAnalyticsAsync(string merchantId, PaymentAnalyticsRequestDto request);
        Task<List<PaymentResponseDto>> GetPaymentHistoryAsync(string userId, int page = 1, int limit = 20);
        
        // Utility Functions
        Task<string> GenerateQrCodeAsync(string upiUrl);
        Task ExpireOldPaymentsAsync();
        Task<List<string>> GetSupportedPaymentMethodsAsync();
        Task<Dictionary<string, decimal>> GetPaymentMethodFeesAsync();
    }

    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentGatewayService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        // Configuration constants - now configurable via appsettings
        private int PaymentValidityMinutes => _configuration.GetValue<int>("PaymentGateway:DefaultValidityMinutes", 15);
        private int MaxRetryAttempts => _configuration.GetValue<int>("Payment:MaxRetryAttempts", 3);
        private decimal MaxTransactionAmount => _configuration.GetValue<decimal>("PaymentGateway:MaxTransactionAmount", 10000000.0m);
        private decimal MinTransactionAmount => _configuration.GetValue<decimal>("PaymentGateway:MinTransactionAmount", 0.01m);
        private const string UPI_SCHEME = "upi://pay";

        public PaymentGatewayService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PaymentGatewayService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request, string userId)
        {
            try
            {
                _logger.LogInformation("Creating payment for user {UserId}, amount {Amount}", userId, request.Amount);

                // Generate unique IDs
                var paymentId = GeneratePaymentId();
                var orderId = GenerateOrderId();

                // Get payment configuration
                var config = await GetPaymentConfigurationAsync();

                // Create payment record
                var payment = new Payment
                {
                    PaymentId = paymentId,
                    OrderId = orderId,
                    UserId = int.Parse(userId),
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = PaymentStatus.Created,
                    PaymentType = request.PaymentType,
                    PaymentMethod = "UPI",
                    Description = request.Description,
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    CustomerPhone = request.CustomerPhone,
                    UpiId = config.UpiId,
                    PayeeName = config.PayeeName,
                    MerchantCode = config.MerchantCode,
                    SuccessUrl = request.SuccessUrl,
                    FailureUrl = request.FailureUrl,
                    CancelUrl = request.CancelUrl,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(PaymentValidityMinutes),
                    Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
                };

                // Generate UPI details
                var upiDetails = await GenerateUpiDetailsAsync(payment, config);
                payment.UpiIntentUrl = upiDetails.UpiIntentUrl;

                // Save payment
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Log status change
                LogPaymentStatusChange(payment.Id, PaymentStatus.Created, PaymentStatus.Created, "Payment created");

                // Generate QR code
                var qrCode = await GenerateQrCodeAsync(upiDetails.UpiUrl);

                // Prepare response
                var response = new PaymentResponseDto
                {
                    PaymentId = paymentId,
                    OrderId = orderId,
                    Status = "CREATED",
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentType = request.PaymentType,
                    UpiDetails = upiDetails,
                    PaymentUrl = upiDetails.UpiIntentUrl,
                    QrCodeBase64 = qrCode,
                    CreatedAt = payment.CreatedAt,
                    ExpiresAt = payment.ExpiresAt,
                    ValidityMinutes = PaymentValidityMinutes,
                    SupportedApps = GetSupportedUpiApps()
                };

                _logger.LogInformation("Payment created successfully: {PaymentId}", paymentId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for user {UserId}", userId);
                throw new Exception("Failed to create payment", ex);
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
                    throw new ArgumentException($"Payment not found: {paymentId}");
                }

                // Check if payment has expired
                if (payment.Status == PaymentStatus.Created || payment.Status == PaymentStatus.Pending)
                {
                    if (DateTime.UtcNow > payment.ExpiresAt)
                    {
                        await UpdatePaymentStatusAsync(payment.Id, PaymentStatus.Expired, "Payment expired");
                        payment.Status = PaymentStatus.Expired;
                    }
                }

                var response = new PaymentStatusResponseDto
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId,
                    Status = payment.Status,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    PaymentMethod = payment.PaymentMethod ?? "UPI",
                    TransactionId = payment.TransactionId,
                    UpiTransactionId = payment.UpiTransactionId,
                    BankTransactionId = payment.BankTransactionId,
                    ReferenceNumber = payment.ReferenceNumber,
                    CreatedAt = payment.CreatedAt,
                    CompletedAt = payment.CompletedAt,
                    FailedAt = payment.FailedAt,
                    ErrorCode = payment.ErrorCode,
                    ErrorMessage = payment.ErrorMessage,
                    FailureReason = payment.FailureReason,
                    CustomerName = payment.CustomerName,
                    CustomerEmail = payment.CustomerEmail,
                    CustomerPhone = payment.CustomerPhone,
                    Metadata = !string.IsNullOrEmpty(payment.Metadata) 
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(payment.Metadata) 
                        : null
                };

                // Generate receipt for successful payments
                if (payment.Status == PaymentStatus.Success)
                {
                    response.Receipt = await GenerateReceiptAsync(payment);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<PaymentStatusResponseDto> VerifyPaymentAsync(VerifyPaymentDto request)
        {
            try
            {
                _logger.LogInformation("Verifying payment {PaymentId} with transaction {TransactionId}", 
                    request.PaymentId, request.TransactionId);

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);

                if (payment == null)
                {
                    throw new ArgumentException($"Payment not found: {request.PaymentId}");
                }

                // Verify payment amount if provided
                if (request.PaidAmount.HasValue && Math.Abs(payment.Amount - request.PaidAmount.Value) > 0.01m)
                {
                    _logger.LogWarning("Payment amount mismatch for {PaymentId}. Expected: {Expected}, Paid: {Paid}",
                        request.PaymentId, payment.Amount, request.PaidAmount);
                    
                    await UpdatePaymentStatusAsync(payment.Id, PaymentStatus.Failed, 
                        $"Amount mismatch. Expected: {payment.Amount}, Paid: {request.PaidAmount}");
                    
                    throw new ArgumentException("Payment amount mismatch");
                }

                // Update payment with transaction details
                payment.Status = PaymentStatus.Success;
                payment.TransactionId = request.TransactionId;
                payment.UpiTransactionId = request.UpiTransactionId;
                payment.BankTransactionId = request.BankTransactionId;
                payment.ReferenceNumber = request.ReferenceNumber;
                payment.PayerVpa = request.PayerVpa;
                payment.CompletedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log status change
                LogPaymentStatusChange(payment.Id, PaymentStatus.Success, 
                    PaymentStatus.Pending, "Payment verified and completed");

                // Process post-payment actions (e.g., create shop)
                await ProcessPostPaymentActionsAsync(payment);

                _logger.LogInformation("Payment verified successfully: {PaymentId}", request.PaymentId);

                return await GetPaymentStatusAsync(request.PaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment {PaymentId}", request.PaymentId);
                throw;
            }
        }

        public async Task<RefundResponseDto> CreateRefundAsync(CreateRefundRequestDto request)
        {
            try
            {
                _logger.LogInformation("Creating refund for payment {PaymentId}, amount {Amount}", 
                    request.PaymentId, request.RefundAmount);

                var payment = await _context.Payments
                    .Include(p => p.Refunds)
                    .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);

                if (payment == null)
                {
                    throw new ArgumentException($"Payment not found: {request.PaymentId}");
                }

                if (payment.Status != PaymentStatus.Success)
                {
                    throw new InvalidOperationException($"Cannot refund payment with status: {payment.Status}");
                }

                // Check refund amount
                var totalRefunded = payment.Refunds.Where(r => r.Status == RefundStatus.Success).Sum(r => r.RefundAmount);
                var remainingAmount = payment.Amount - totalRefunded;

                if (request.RefundAmount > remainingAmount)
                {
                    throw new ArgumentException($"Refund amount exceeds remaining refundable amount: {remainingAmount}");
                }

                // Generate refund ID
                var refundId = request.RefundId ?? GenerateRefundId();

                // Create refund record
                var refund = new PaymentRefund
                {
                    RefundId = refundId,
                    PaymentId = payment.Id,
                    RefundAmount = request.RefundAmount,
                    Currency = payment.Currency,
                    Status = RefundStatus.Created,
                    Reason = request.Reason,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
                };

                _context.PaymentRefunds.Add(refund);

                // Update payment status
                var newPaymentStatus = (totalRefunded + request.RefundAmount >= payment.Amount) 
                    ? PaymentStatus.Refunded 
                    : PaymentStatus.PartiallyRefunded;

                payment.Status = newPaymentStatus;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // In a real implementation, you would integrate with the payment processor's refund API here
                // For now, we'll mark the refund as successful
                refund.Status = RefundStatus.Success;
                refund.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var response = new RefundResponseDto
                {
                    RefundId = refundId,
                    PaymentId = request.PaymentId,
                    Status = RefundStatus.Success,
                    RefundAmount = request.RefundAmount,
                    Currency = payment.Currency,
                    Reason = request.Reason,
                    CreatedAt = refund.CreatedAt,
                    ProcessedAt = refund.ProcessedAt
                };

                _logger.LogInformation("Refund created successfully: {RefundId}", refundId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund for payment {PaymentId}", request.PaymentId);
                throw;
            }
        }

        public async Task<bool> ProcessWebhookAsync(PaymentWebhookDto webhook)
        {
            try
            {
                _logger.LogInformation("Processing webhook for payment {PaymentId}, event {Event}", 
                    webhook.PaymentId, webhook.Event);

                // Verify webhook signature
                if (!VerifyWebhookSignature(webhook))
                {
                    _logger.LogWarning("Invalid webhook signature for payment {PaymentId}", webhook.PaymentId);
                    return false;
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.PaymentId == webhook.PaymentId);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for webhook: {PaymentId}", webhook.PaymentId);
                    return false;
                }

                // Process based on event type
                switch (webhook.Event.ToLower())
                {
                    case "payment.success":
                        await HandlePaymentSuccessWebhook(payment, webhook);
                        break;
                    case "payment.failed":
                        await HandlePaymentFailedWebhook(payment, webhook);
                        break;
                    case "payment.pending":
                        await HandlePaymentPendingWebhook(payment, webhook);
                        break;
                    default:
                        _logger.LogWarning("Unknown webhook event: {Event}", webhook.Event);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook for payment {PaymentId}", webhook.PaymentId);
                return false;
            }
        }

        public async Task<PaymentAnalyticsResponseDto> GetAnalyticsAsync(PaymentAnalyticsRequestDto request)
        {
            try
            {
                var query = _context.Payments.AsQueryable();

                // Apply filters
                if (request.StartDate.HasValue)
                    query = query.Where(p => p.CreatedAt >= request.StartDate.Value);

                if (request.EndDate.HasValue)
                    query = query.Where(p => p.CreatedAt <= request.EndDate.Value);

                if (!string.IsNullOrEmpty(request.PaymentType))
                    query = query.Where(p => p.PaymentType == request.PaymentType);

                if (request.Status.HasValue)
                    query = query.Where(p => p.Status == request.Status.Value);

                var payments = await query.ToListAsync();

                var analytics = new PaymentAnalyticsResponseDto
                {
                    TotalAmount = payments.Sum(p => p.Amount),
                    TotalTransactions = payments.Count,
                    SuccessfulTransactions = payments.Count(p => p.Status == PaymentStatus.Success),
                    FailedTransactions = payments.Count(p => p.Status == PaymentStatus.Failed),
                    AverageTransactionAmount = payments.Any() ? payments.Average(p => p.Amount) : 0
                };

                analytics.SuccessRate = analytics.TotalTransactions > 0 
                    ? (decimal)analytics.SuccessfulTransactions / analytics.TotalTransactions * 100 
                    : 0;

                // Generate trends based on groupBy parameter
                analytics.Trends = GeneratePaymentTrends(payments, request.GroupBy ?? "day");

                // Status breakdown
                analytics.StatusBreakdown = payments
                    .GroupBy(p => p.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                // Payment method breakdown
                analytics.PaymentMethodBreakdown = payments
                    .GroupBy(p => p.PaymentMethod ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment analytics");
                throw;
            }
        }

        public Task<string> GenerateQrCodeAsync(string upiUrl)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(upiUrl, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);
                return Task.FromResult(Convert.ToBase64String(qrCodeBytes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                throw;
            }
        }

        public async Task ExpireOldPaymentsAsync()
        {
            try
            {
                var expiredPayments = await _context.Payments
                    .Where(p => (p.Status == PaymentStatus.Created || p.Status == PaymentStatus.Pending) 
                               && p.ExpiresAt < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var payment in expiredPayments)
                {
                    await UpdatePaymentStatusAsync(payment.Id, PaymentStatus.Expired, "Payment expired automatically");
                }

                if (expiredPayments.Any())
                {
                    _logger.LogInformation("Expired {Count} old payments", expiredPayments.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring old payments");
            }
        }

        // Private helper methods
        private Task<UpiPaymentDetails> GenerateUpiDetailsAsync(Payment payment, PaymentConfig config)
        {
            var transactionNote = $"{payment.PaymentType}_{payment.PaymentId}";
            var upiUrl = $"{UPI_SCHEME}?pa={config.UpiId}&pn={Uri.EscapeDataString(config.PayeeName)}&am={payment.Amount}&cu={payment.Currency}&tn={Uri.EscapeDataString(transactionNote)}";
            var upiIntentUrl = $"upi://pay?pa={config.UpiId}&pn={Uri.EscapeDataString(config.PayeeName)}&am={payment.Amount}&cu={payment.Currency}&tn={Uri.EscapeDataString(transactionNote)}&mode=02&purpose=00";

            return Task.FromResult(new UpiPaymentDetails
            {
                UpiId = config.UpiId,
                PayeeName = config.PayeeName,
                MerchantCode = config.MerchantCode,
                TransactionNote = transactionNote,
                UpiIntentUrl = upiIntentUrl,
                UpiUrl = upiUrl
            });
        }

        private List<UpiAppInfo> GetSupportedUpiApps()
        {
            return new List<UpiAppInfo>
            {
                new UpiAppInfo { Name = "Google Pay", PackageName = "com.google.android.apps.nbu.paisa.user", IsRecommended = true },
                new UpiAppInfo { Name = "PhonePe", PackageName = "com.phonepe.app", IsRecommended = true },
                new UpiAppInfo { Name = "Paytm", PackageName = "net.one97.paytm", IsRecommended = true },
                new UpiAppInfo { Name = "BHIM", PackageName = "in.org.npci.upiapp", IsRecommended = false },
                new UpiAppInfo { Name = "Amazon Pay", PackageName = "in.amazon.mShop.android.shopping", IsRecommended = false }
            };
        }

        private Task<PaymentConfig> GetPaymentConfigurationAsync()
        {
            return Task.FromResult(new PaymentConfig
            {
                UpiId = _configuration["Payment:UpiId"] ?? "tishnut@fifderal",
                PayeeName = _configuration["Payment:PayeeName"] ?? "STIBE BUSINESS",
                MerchantCode = _configuration["Payment:MerchantCode"] ?? "STIBE001"
            });
        }

        private string GeneratePaymentId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = new Random().Next(1000, 9999);
            return $"PAY_{timestamp}_{random}";
        }

        private string GenerateOrderId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = new Random().Next(10000, 99999);
            return $"ORD_{timestamp}_{random}";
        }

        private string GenerateRefundId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = new Random().Next(1000, 9999);
            return $"REF_{timestamp}_{random}";
        }

        private async Task UpdatePaymentStatusAsync(int paymentId, PaymentStatus newStatus, string remarks)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return;

            var previousStatus = payment.Status;
            payment.Status = newStatus;
            payment.UpdatedAt = DateTime.UtcNow;

            if (newStatus == PaymentStatus.Success)
                payment.CompletedAt = DateTime.UtcNow;
            else if (newStatus == PaymentStatus.Failed || newStatus == PaymentStatus.Expired)
                payment.FailedAt = DateTime.UtcNow;

            LogPaymentStatusChange(paymentId, newStatus, previousStatus, remarks);
            await _context.SaveChangesAsync();
        }

        private void LogPaymentStatusChange(int paymentId, PaymentStatus newStatus, PaymentStatus previousStatus, string remarks)
        {
            var statusHistory = new PaymentStatusHistory
            {
                PaymentId = paymentId,
                Status = newStatus,
                PreviousStatus = previousStatus,
                Remarks = remarks,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentStatusHistories.Add(statusHistory);
        }

        private async Task ProcessPostPaymentActionsAsync(Payment payment)
        {
            try
            {
                if (payment.PaymentType == "SHOP_REGISTRATION" && !string.IsNullOrEmpty(payment.Metadata))
                {
                    // Parse shop data from metadata
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(payment.Metadata);
                    if (metadata != null && metadata.ContainsKey("shopData"))
                    {
                        // Extract shop creation information from metadata
                        _logger.LogInformation("Shop registration payment completed, shop data will be processed by shop service");
                        
                        // Update payment to link with the shop that will be created
                        // The actual shop creation should be handled by a dedicated shop service
                        payment.CreatedShopId = null; // Will be updated by shop service
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing post-payment actions for {PaymentId}", payment.PaymentId);
            }
        }

        private Task<PaymentReceiptDto> GenerateReceiptAsync(Payment payment)
        {
            return Task.FromResult(new PaymentReceiptDto
            {
                ReceiptId = $"RCP_{payment.PaymentId}",
                PaymentId = payment.PaymentId,
                TransactionId = payment.TransactionId ?? "",
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod ?? "UPI",
                PaymentDate = payment.CompletedAt ?? payment.CreatedAt,
                CustomerName = payment.CustomerName ?? "",
                CustomerEmail = payment.CustomerEmail ?? "",
                MerchantName = payment.PayeeName ?? "STIBE BUSINESS",
                Description = payment.Description ?? payment.PaymentType
            });
        }

        private bool VerifyWebhookSignature(PaymentWebhookDto webhook)
        {
            // In a real implementation, you would verify the webhook signature
            // using your secret key and the payload
            return true;
        }

        private async Task HandlePaymentSuccessWebhook(Payment payment, PaymentWebhookDto webhook)
        {
            if (payment.Status != PaymentStatus.Success)
            {
                payment.Status = PaymentStatus.Success;
                payment.TransactionId = webhook.TransactionId;
                payment.CompletedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await ProcessPostPaymentActionsAsync(payment);
                await _context.SaveChangesAsync();
            }
        }

        private async Task HandlePaymentFailedWebhook(Payment payment, PaymentWebhookDto webhook)
        {
            if (payment.Status != PaymentStatus.Failed)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = webhook.FailureReason;
                payment.FailedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }

        private async Task HandlePaymentPendingWebhook(Payment payment, PaymentWebhookDto webhook)
        {
            if (payment.Status == PaymentStatus.Created)
            {
                payment.Status = PaymentStatus.Pending;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }

        private List<PaymentTrendDto> GeneratePaymentTrends(List<Payment> payments, string groupBy)
        {
            return groupBy.ToLower() switch
            {
                "day" => payments.GroupBy(p => p.CreatedAt.Date)
                    .Select(g => new PaymentTrendDto
                    {
                        Date = g.Key,
                        Amount = g.Sum(p => p.Amount),
                        Count = g.Count()
                    }).OrderBy(t => t.Date).ToList(),
                "week" => payments.GroupBy(p => GetWeekStart(p.CreatedAt))
                    .Select(g => new PaymentTrendDto
                    {
                        Date = g.Key,
                        Amount = g.Sum(p => p.Amount),
                        Count = g.Count()
                    }).OrderBy(t => t.Date).ToList(),
                "month" => payments.GroupBy(p => new DateTime(p.CreatedAt.Year, p.CreatedAt.Month, 1))
                    .Select(g => new PaymentTrendDto
                    {
                        Date = g.Key,
                        Amount = g.Sum(p => p.Amount),
                        Count = g.Count()
                    }).OrderBy(t => t.Date).ToList(),
                _ => new List<PaymentTrendDto>()
            };
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        // Universal Payment Type Implementations

        public async Task<PaymentResponseDto> CreateServiceBookingPaymentAsync(ServiceBookingPaymentDto request, string userId)
        {
            var baseRequest = new CreatePaymentRequestDto
            {
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentType = "SERVICE_BOOKING",
                PaymentCategory = "SERVICE",
                Description = $"Service booking: {request.Description}",
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                ServiceId = request.ServiceProviderId,
                Metadata = request.Metadata,
                SuccessUrl = request.SuccessUrl,
                FailureUrl = request.FailureUrl,
                CancelUrl = request.CancelUrl
            };

            return await CreatePaymentAsync(baseRequest, userId);
        }

        public async Task<PaymentResponseDto> CreateMarketplacePaymentAsync(MarketplacePaymentDto request, string userId)
        {
            var baseRequest = new CreatePaymentRequestDto
            {
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentType = "MARKETPLACE_ORDER",
                PaymentCategory = "MARKETPLACE",
                Description = $"Marketplace order: {request.Description}",
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                MerchantId = request.SellerId,
                Metadata = request.Metadata,
                SuccessUrl = request.SuccessUrl,
                FailureUrl = request.FailureUrl,
                CancelUrl = request.CancelUrl
            };

            return await CreatePaymentAsync(baseRequest, userId);
        }

        public async Task<PaymentResponseDto> CreateSubscriptionPaymentAsync(SubscriptionPaymentDto request, string userId)
        {
            var baseRequest = new CreatePaymentRequestDto
            {
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentType = request.BillingCycle == "YEARLY" ? "YEARLY_SUBSCRIPTION" : "MONTHLY_SUBSCRIPTION",
                PaymentCategory = "SUBSCRIPTION",
                Description = $"Subscription: {request.PlanName}",
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                SubscriptionId = request.PlanId,
                Metadata = request.Metadata,
                SuccessUrl = request.SuccessUrl,
                FailureUrl = request.FailureUrl,
                CancelUrl = request.CancelUrl
            };

            return await CreatePaymentAsync(baseRequest, userId);
        }

        public async Task<PaymentResponseDto> CreateVendorPaymentAsync(VendorPaymentDto request, string userId)
        {
            var baseRequest = new CreatePaymentRequestDto
            {
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentType = "VENDOR_PAYMENT",
                PaymentCategory = "VENDOR",
                Description = $"Vendor payment: {request.PaymentPurpose}",
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                VendorId = request.PayeeVendorId,
                Metadata = request.Metadata,
                SuccessUrl = request.SuccessUrl,
                FailureUrl = request.FailureUrl,
                CancelUrl = request.CancelUrl
            };

            return await CreatePaymentAsync(baseRequest, userId);
        }

        public async Task<PaymentLinkResponseDto> CreatePaymentLinkAsync(CreatePaymentLinkDto request, string userId)
        {
            try
            {
                var linkId = $"PL_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{new Random().Next(1000, 9999)}";
                var paymentResponse = await CreatePaymentAsync(request.PaymentDetails, userId);
                
                var linkUrl = $"{_configuration["App:BaseUrl"]}/pay/{linkId}";
                var shortUrl = await GenerateShortUrlAsync(linkUrl);
                var qrCode = await GenerateQrCodeAsync(linkUrl);

                return new PaymentLinkResponseDto
                {
                    LinkId = linkId,
                    PaymentLinkUrl = linkUrl,
                    ShortUrl = shortUrl,
                    QrCodeBase64 = qrCode,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = request.ExpiryDate,
                    Status = "ACTIVE"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link");
                throw new InvalidOperationException("Failed to create payment link");
            }
        }

        public Task<PaymentResponseDto> PayUsingLinkAsync(string linkId, string customerId)
        {
            // This would implement payment using a payment link
            // For now, return a basic implementation
            throw new NotImplementedException("Payment link functionality will be implemented in next phase");
        }

        public async Task<PaymentAnalyticsResponseDto> GetMerchantAnalyticsAsync(string merchantId, PaymentAnalyticsRequestDto request)
        {
            try
            {
                var query = _context.Payments.Where(p => p.MerchantId == merchantId);

                // Apply filters
                if (request.StartDate.HasValue)
                    query = query.Where(p => p.CreatedAt >= request.StartDate.Value);

                if (request.EndDate.HasValue)
                    query = query.Where(p => p.CreatedAt <= request.EndDate.Value);

                if (!string.IsNullOrEmpty(request.PaymentType))
                    query = query.Where(p => p.PaymentType == request.PaymentType);

                if (request.Status.HasValue)
                    query = query.Where(p => p.Status == request.Status.Value);

                var payments = await query.ToListAsync();

                return new PaymentAnalyticsResponseDto
                {
                    TotalAmount = payments.Sum(p => p.Amount),
                    TotalTransactions = payments.Count,
                    SuccessfulTransactions = payments.Count(p => p.Status == PaymentStatus.Success),
                    FailedTransactions = payments.Count(p => p.Status == PaymentStatus.Failed),
                    SuccessRate = payments.Count > 0 ? (decimal)payments.Count(p => p.Status == PaymentStatus.Success) / payments.Count * 100 : 0,
                    AverageTransactionAmount = payments.Count > 0 ? payments.Average(p => p.Amount) : 0,
                    Trends = GeneratePaymentTrends(payments, request.GroupBy ?? "day"),
                    StatusBreakdown = payments.GroupBy(p => p.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
                    PaymentMethodBreakdown = payments.GroupBy(p => p.PaymentMethod ?? "UPI").ToDictionary(g => g.Key, g => g.Sum(p => p.Amount))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting merchant analytics for merchant {MerchantId}", merchantId);
                throw new InvalidOperationException("Failed to retrieve merchant analytics");
            }
        }

        public async Task<List<PaymentResponseDto>> GetPaymentHistoryAsync(string userId, int page = 1, int limit = 20)
        {
            try
            {
                var payments = await _context.Payments
                    .Where(p => p.UserId.ToString() == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                return payments.Select(p => new PaymentResponseDto
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId,
                    Status = p.Status.ToString(),
                    Amount = p.Amount,
                    Currency = p.Currency,
                    PaymentType = p.PaymentType,
                    CreatedAt = p.CreatedAt,
                    ExpiresAt = p.ExpiresAt,
                    ValidityMinutes = PaymentValidityMinutes
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment history for user {UserId}", userId);
                throw new InvalidOperationException("Failed to retrieve payment history");
            }
        }

        public async Task<List<string>> GetSupportedPaymentMethodsAsync()
        {
            var methods = _configuration.GetSection("Payment:SupportedMethods").Get<List<string>>();
            return await Task.FromResult(methods ?? new List<string> { "UPI", "NET_BANKING", "DEBIT_CARD", "CREDIT_CARD", "WALLET", "EMI" });
        }

        public async Task<Dictionary<string, decimal>> GetPaymentMethodFeesAsync()
        {
            var fees = _configuration.GetSection("Payment:MethodFees").Get<Dictionary<string, decimal>>();
            return await Task.FromResult(fees ?? new Dictionary<string, decimal>
            {
                { "UPI", 0.0m },
                { "NET_BANKING", 2.0m },
                { "DEBIT_CARD", 1.5m },
                { "CREDIT_CARD", 2.5m },
                { "WALLET", 1.0m },
                { "EMI", 3.0m }
            });
        }

        private async Task<string> GenerateShortUrlAsync(string longUrl)
        {
            // Simple short URL implementation
            var shortCode = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("=", "").Replace("+", "").Replace("/", "").Substring(0, 8);
            return await Task.FromResult($"{_configuration["App:BaseUrl"]}/s/{shortCode}");
        }

        private class PaymentConfig
        {
            public string UpiId { get; set; } = "";
            public string PayeeName { get; set; } = "";
            public string MerchantCode { get; set; } = "";
        }
    }
}
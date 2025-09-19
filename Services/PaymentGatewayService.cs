using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs;
using stibe.api.Models.Entities;
using System.Text.Json;

namespace stibe.api.Services
{
    public interface IPaymentGatewayService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request, string userId);
        Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string paymentId);
        Task<PaymentStatusResponseDto> VerifyPaymentAsync(VerifyPaymentDto request);
        Task<RefundResponseDto> CreateRefundAsync(CreateRefundRequestDto request);
        Task<PaymentAnalyticsResponseDto> GetAnalyticsAsync(PaymentAnalyticsRequestDto request);
        Task<bool> ProcessWebhookAsync(PaymentWebhookDto webhook);
        Task<string> GenerateQrCodeAsync(string upiUrl);
        
        // Universal payment methods
        Task<PaymentResponseDto> CreateServiceBookingPaymentAsync(ServiceBookingPaymentDto request, string userId);
        Task<PaymentResponseDto> CreateMarketplacePaymentAsync(MarketplacePaymentDto request, string userId);
        Task<PaymentResponseDto> CreateSubscriptionPaymentAsync(SubscriptionPaymentDto request, string userId);
        Task<PaymentResponseDto> CreateVendorPaymentAsync(VendorPaymentDto request, string userId);
        Task<PaymentLinkResponseDto> CreatePaymentLinkAsync(CreatePaymentLinkDto request, string userId);
        Task<PaymentAnalyticsResponseDto> GetMerchantAnalyticsAsync(string merchantId, PaymentAnalyticsRequestDto request);
        Task<List<PaymentResponseDto>> GetPaymentHistoryAsync(string userId, int page, int limit);
        Task<List<string>> GetSupportedPaymentMethodsAsync();
        Task<Dictionary<string, decimal>> GetPaymentMethodFeesAsync();
    }

    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentGatewayService> _logger;

        public PaymentGatewayService(ApplicationDbContext context, ILogger<PaymentGatewayService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto request, string userId)
        {
            try
            {
                var paymentId = GeneratePaymentId();
                
                var upiUrl = GenerateUpiUrl(paymentId, request.Amount, request.Description ?? $"Payment for {request.PaymentType}");
                
                var payment = new Payment
                {
                    PaymentId = paymentId,
                    UserId = request.UserId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Purpose = request.PaymentType,
                    Description = request.Description,
                    Status = "PENDING",
                    PaymentMethod = "UPI",
                    UpiId = "tishnut@fifderal",
                    PayeeName = "STIBE BUSINESS",
                    UpiIntentUrl = upiUrl,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return new PaymentResponseDto
                {
                    PaymentId = paymentId,
                    Status = "PENDING",
                    Amount = request.Amount,
                    Currency = request.Currency,
                    UpiDetails = new UpiPaymentDetails
                    {
                        UpiId = "tishnut@fifderal",
                        PayeeName = "STIBE BUSINESS",
                        UpiIntentUrl = upiUrl
                    },
                    QrCodeBase64 = GenerateQrCodeBase64(upiUrl),
                    ExpiresAt = payment.ExpiresAt,
                    CreatedAt = payment.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                throw;
            }
        }

        public async Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string paymentId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                throw new ArgumentException("Payment not found");

            // Check if payment has expired
            if (DateTime.UtcNow > payment.ExpiresAt && payment.Status == "PENDING")
            {
                payment.Status = "EXPIRED";
                payment.FailureReason = "Payment expired";
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Convert string status to enum for response
            PaymentStatus status = payment.Status switch
            {
                "PENDING" => PaymentStatus.Pending,
                "SUCCESS" => PaymentStatus.Success,
                "FAILED" => PaymentStatus.Failed,
                "EXPIRED" => PaymentStatus.Expired,
                _ => PaymentStatus.Pending
            };

            return new PaymentStatusResponseDto
            {
                PaymentId = payment.PaymentId,
                Status = status,
                Amount = payment.Amount,
                Currency = payment.Currency,
                UpiTransactionId = payment.UpiTransactionRef,
                TransactionId = payment.TransactionId,
                CreatedAt = payment.CreatedAt,
                CompletedAt = payment.CompletedAt,
                FailureReason = payment.FailureReason
            };
        }

        public async Task<PaymentStatusResponseDto> VerifyPaymentAsync(VerifyPaymentDto request)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId);

            if (payment == null)
                throw new ArgumentException("Payment not found");

            if (payment.Status == "SUCCESS")
                throw new InvalidOperationException("Payment already verified");

            if (payment.Status != "PENDING")
                throw new InvalidOperationException($"Payment cannot be verified in {payment.Status} status");

            // Mark payment as successful
            payment.Status = "SUCCESS";
            payment.TransactionId = request.TransactionId;
            payment.UpiTransactionRef = request.UpiTransactionId;
            payment.CompletedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new PaymentStatusResponseDto
            {
                PaymentId = payment.PaymentId,
                Status = PaymentStatus.Success,
                Amount = payment.Amount,
                Currency = payment.Currency,
                UpiTransactionId = payment.UpiTransactionRef,
                TransactionId = payment.TransactionId,
                CreatedAt = payment.CreatedAt,
                CompletedAt = payment.CompletedAt
            };
        }

        // Simple implementations for the required interface methods
        public Task<RefundResponseDto> CreateRefundAsync(CreateRefundRequestDto request)
        {
            throw new NotImplementedException("Refunds not implemented in simplified version");
        }

        public Task<PaymentAnalyticsResponseDto> GetAnalyticsAsync(PaymentAnalyticsRequestDto request)
        {
            throw new NotImplementedException("Analytics not implemented in simplified version");
        }

        public Task<bool> ProcessWebhookAsync(PaymentWebhookDto webhook)
        {
            return Task.FromResult(true); // Simplified implementation
        }

        public Task<string> GenerateQrCodeAsync(string upiUrl)
        {
            return Task.FromResult(GenerateQrCodeBase64(upiUrl));
        }

        public Task<PaymentResponseDto> CreateServiceBookingPaymentAsync(ServiceBookingPaymentDto request, string userId)
        {
            throw new NotImplementedException("Service booking payments not implemented in simplified version");
        }

        public Task<PaymentResponseDto> CreateMarketplacePaymentAsync(MarketplacePaymentDto request, string userId)
        {
            throw new NotImplementedException("Marketplace payments not implemented in simplified version");
        }

        public Task<PaymentResponseDto> CreateSubscriptionPaymentAsync(SubscriptionPaymentDto request, string userId)
        {
            throw new NotImplementedException("Subscription payments not implemented in simplified version");
        }

        public Task<PaymentResponseDto> CreateVendorPaymentAsync(VendorPaymentDto request, string userId)
        {
            throw new NotImplementedException("Vendor payments not implemented in simplified version");
        }

        public Task<PaymentLinkResponseDto> CreatePaymentLinkAsync(CreatePaymentLinkDto request, string userId)
        {
            throw new NotImplementedException("Payment links not implemented in simplified version");
        }

        public Task<PaymentAnalyticsResponseDto> GetMerchantAnalyticsAsync(string merchantId, PaymentAnalyticsRequestDto request)
        {
            throw new NotImplementedException("Merchant analytics not implemented in simplified version");
        }

        public async Task<List<PaymentResponseDto>> GetPaymentHistoryAsync(string userId, int page, int limit)
        {
            var payments = await _context.Payments
                .Where(p => p.UserId == int.Parse(userId))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return payments.Select(p => new PaymentResponseDto
            {
                PaymentId = p.PaymentId,
                Status = p.Status,
                Amount = p.Amount,
                Currency = p.Currency,
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public Task<List<string>> GetSupportedPaymentMethodsAsync()
        {
            return Task.FromResult(new List<string> { "UPI" });
        }

        public Task<Dictionary<string, decimal>> GetPaymentMethodFeesAsync()
        {
            return Task.FromResult(new Dictionary<string, decimal> { { "UPI", 0.0m } });
        }

        // Helper methods
        private string GeneratePaymentId()
        {
            return $"PAY_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 50);
        }

        private string GenerateUpiUrl(string paymentId, decimal amount, string description)
        {
            var upiId = "tishnut@fifderal";
            var payeeName = "STIBE BUSINESS";
            var merchantCode = "STIBE001";
            
            return $"upi://pay?pa={upiId}&pn={Uri.EscapeDataString(payeeName)}&mc={merchantCode}&tid={paymentId}&tr={paymentId}&tn={Uri.EscapeDataString(description)}&am={amount}&cu=INR";
        }

        private string GenerateQrCodeBase64(string upiUrl)
        {
            // This is a placeholder - in a real implementation, you would use a QR code library
            return "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
        }
    }
}
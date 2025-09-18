using System.ComponentModel.DataAnnotations;
using stibe.api.Models.Entities;

namespace stibe.api.Models.DTOs
{
    /// <summary>
    /// Professional Payment Gateway DTOs for production-ready UPI integration
    /// </summary>
    
    // Universal Payment Request for all STIBE transactions
    public class CreatePaymentRequestDto
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [Range(0.01, 10000000.0, ErrorMessage = "Amount must be between ₹0.01 and ₹1,00,00,000")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "INR";
        
        [Required]
        [StringLength(50)]
        public string PaymentType { get; set; } = "GENERAL";
        
        [StringLength(50)]
        public string PaymentCategory { get; set; } = "GENERAL";
        
        [StringLength(50)]
        public string PreferredPaymentMethod { get; set; } = "UPI";
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        // Enhanced metadata for different payment types
        [StringLength(100)]
        public string? MerchantId { get; set; } // For marketplace transactions
        
        [StringLength(100)]
        public string? VendorId { get; set; } // For vendor payments
        
        [StringLength(100)]
        public string? ServiceId { get; set; } // For service bookings
        
        [StringLength(100)]
        public string? SubscriptionId { get; set; } // For subscription payments
        
        [StringLength(100)]
        public string? OrderId { get; set; } // External order reference
        
        // Customer Information
        [StringLength(100)]
        public string? CustomerName { get; set; }
        
        [StringLength(100)]
        [EmailAddress]
        public string? CustomerEmail { get; set; }
        
        [StringLength(15)]
        public string? CustomerPhone { get; set; }
        
        // Additional metadata
        public Dictionary<string, string> Metadata { get; set; } = new();
        
        // Payment flow options
        public bool SendEmailNotification { get; set; } = true;
        public bool SendSmsNotification { get; set; } = false;
        public bool AutoCapture { get; set; } = true;
        public int ValidityMinutes { get; set; } = 30;
        
        // Return URLs for webhook/callback
        [Url]
        public string? SuccessUrl { get; set; }
        
        [Url]
        public string? FailureUrl { get; set; }
        
        [Url]
        public string? CancelUrl { get; set; }
    }

    // Enhanced Payment Response with more details
    public class PaymentResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = "CREATED";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string PaymentType { get; set; } = string.Empty;
        
        // UPI specific details
        public UpiPaymentDetails UpiDetails { get; set; } = new();
        
        // Payment links and QR
        public string PaymentUrl { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
        
        // Timing
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int ValidityMinutes { get; set; }
        
        // Supported apps
        public List<UpiAppInfo> SupportedApps { get; set; } = new();
    }

    public class UpiPaymentDetails
    {
        public string UpiId { get; set; } = string.Empty;
        public string PayeeName { get; set; } = string.Empty;
        public string MerchantCode { get; set; } = string.Empty;
        public string TransactionNote { get; set; } = string.Empty;
        public string UpiIntentUrl { get; set; } = string.Empty;
        public string UpiUrl { get; set; } = string.Empty;
    }

    public class UpiAppInfo
    {
        public string Name { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string DeepLinkUrl { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
        public bool IsRecommended { get; set; }
    }

    // Payment Status Check
    public class PaymentStatusRequestDto
    {
        [Required]
        public string PaymentId { get; set; } = string.Empty;
        
        public string? OrderId { get; set; }
    }

    public class PaymentStatusResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string PaymentMethod { get; set; } = string.Empty;
        
        // Transaction details
        public string? TransactionId { get; set; }
        public string? UpiTransactionId { get; set; }
        public string? BankTransactionId { get; set; }
        public string? ReferenceNumber { get; set; }
        
        // Timing
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }
        
        // Error details
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FailureReason { get; set; }
        
        // Customer details
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        
        // Additional info
        public Dictionary<string, object>? Metadata { get; set; }
        public PaymentReceiptDto? Receipt { get; set; }
    }

    // Payment Verification
    public class VerifyPaymentDto
    {
        [Required]
        public string PaymentId { get; set; } = string.Empty;
        
        [Required]
        public string TransactionId { get; set; } = string.Empty;
        
        public string? UpiTransactionId { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? BankTransactionId { get; set; }
        public string? PayerVpa { get; set; }
        public decimal? PaidAmount { get; set; }
        public string? Remarks { get; set; }
    }

    // Webhook payload
    public class PaymentWebhookDto
    {
        public string Event { get; set; } = string.Empty; // payment.success, payment.failed, etc.
        public string PaymentId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string? TransactionId { get; set; }
        public string? FailureReason { get; set; }
        public DateTime Timestamp { get; set; }
        public string Signature { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
    }

    // Payment Receipt
    public class PaymentReceiptDto
    {
        public string ReceiptId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? TaxAmount { get; set; }
        public decimal? FeeAmount { get; set; }
        public string? GstNumber { get; set; }
        public string? InvoiceNumber { get; set; }
    }

    // Payment Refund
    public class CreateRefundRequestDto
    {
        [Required]
        public string PaymentId { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal RefundAmount { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? RefundId { get; set; }
        
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class RefundResponseDto
    {
        public string RefundId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public decimal RefundAmount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Analytics and Reporting
    public class PaymentAnalyticsRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PaymentType { get; set; }
        public PaymentStatus? Status { get; set; }
        public string? GroupBy { get; set; } // day, week, month
    }

    public class PaymentAnalyticsResponseDto
    {
        public decimal TotalAmount { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageTransactionAmount { get; set; }
        public List<PaymentTrendDto> Trends { get; set; } = new();
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
        public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; } = new();
    }

    public class PaymentTrendDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    // Universal Payment Type-specific DTOs
    
    // Service Booking Payment
    public class ServiceBookingPaymentDto : CreatePaymentRequestDto
    {
        [Required]
        public string ServiceProviderId { get; set; } = string.Empty;
        
        [Required]
        public DateTime ServiceDate { get; set; }
        
        [StringLength(100)]
        public string ServiceLocation { get; set; } = string.Empty;
        
        public List<ServiceItemDto> ServiceItems { get; set; } = new();
    }

    public class ServiceItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal ItemPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public string ItemDescription { get; set; } = string.Empty;
    }

    // Marketplace Order Payment
    public class MarketplacePaymentDto : CreatePaymentRequestDto
    {
        [Required]
        public string SellerId { get; set; } = string.Empty;
        
        [Required]
        public List<OrderItemDto> OrderItems { get; set; } = new();
        
        public decimal ShippingCharges { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public string ShippingAddress { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string ProductVariant { get; set; } = string.Empty;
    }

    // Subscription Payment
    public class SubscriptionPaymentDto : CreatePaymentRequestDto
    {
        [Required]
        public string PlanId { get; set; } = string.Empty;
        
        [Required]
        public string PlanName { get; set; } = string.Empty;
        
        public string BillingCycle { get; set; } = "MONTHLY"; // MONTHLY, YEARLY
        public DateTime SubscriptionStartDate { get; set; }
        public DateTime SubscriptionEndDate { get; set; }
        public bool IsRecurring { get; set; } = true;
        public decimal SetupFee { get; set; } = 0;
    }

    // Vendor Payment
    public class VendorPaymentDto : CreatePaymentRequestDto
    {
        [Required]
        public string PayeeVendorId { get; set; } = string.Empty;
        
        [Required]
        public string PaymentPurpose { get; set; } = string.Empty; // COMMISSION, SETTLEMENT, BONUS
        
        public string ReferenceTransactionId { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; } = 0;
        public string PayoutMethod { get; set; } = "BANK_TRANSFER";
        public Dictionary<string, string> BankDetails { get; set; } = new();
    }

    // Payment Link DTO
    public class CreatePaymentLinkDto
    {
        [Required]
        public CreatePaymentRequestDto PaymentDetails { get; set; } = new();
        
        [Required]
        [StringLength(100)]
        public string LinkTitle { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string LinkDescription { get; set; } = string.Empty;
        
        public DateTime ExpiryDate { get; set; }
        public bool AllowPartialPayment { get; set; } = false;
        public decimal MinimumAmount { get; set; } = 0;
        public bool SendEmailNotification { get; set; } = true;
        public bool SendSmsNotification { get; set; } = false;
        public string? CustomBranding { get; set; }
    }

    public class PaymentLinkResponseDto
    {
        public string LinkId { get; set; } = string.Empty;
        public string PaymentLinkUrl { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public int ClickCount { get; set; } = 0;
    }

    // Payment Configuration DTO
    public class PaymentConfigDto
    {
        public List<string> SupportedMethods { get; set; } = new();
        public Dictionary<string, decimal> PaymentFees { get; set; } = new();
        public Dictionary<string, string> UpiApps { get; set; } = new();
        public string MerchantUpiId { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public int DefaultValidityMinutes { get; set; } = 30;
        public decimal MinAmount { get; set; } = 1.0m;
        public decimal MaxAmount { get; set; } = 1000000.0m;
        public bool IsEnabled { get; set; } = true;
        
        // Additional properties for compatibility
        public decimal ShopRegistrationFee { get; set; } = 100.0m;
        public string Currency { get; set; } = "INR";
        public string CurrencySymbol { get; set; } = "₹";
        public int PaymentTimeoutMinutes { get; set; } = 30;
        public int MaxRetryAttempts { get; set; } = 3;
        public string UpiPaymentAddress { get; set; } = string.Empty;
        public string PayeeName { get; set; } = string.Empty;
        public string MerchantCode { get; set; } = string.Empty;
        public List<string> SupportedPaymentMethods { get; set; } = new();
        public Dictionary<string, string> SupportedUpiApps { get; set; } = new();
    }

}
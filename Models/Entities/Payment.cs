using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using stibe.api.Models.Entities.PartnersEntity;

namespace stibe.api.Models.Entities
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string PaymentId { get; set; } = string.Empty; // Unique payment identifier
        
        [Required]
        [StringLength(100)]
        public string OrderId { get; set; } = string.Empty; // Order/Transaction identifier
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "INR";
        
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Created;
        
        [Required]
        [StringLength(50)]
        public string PaymentType { get; set; } = string.Empty; // SHOP_REGISTRATION, SERVICE_BOOKING, VENDOR_PAYMENT, SUBSCRIPTION, MARKETPLACE_TRANSACTION, etc.
        
        [StringLength(50)]
        public string PaymentCategory { get; set; } = "GENERAL"; // BUSINESS, CONSUMER, VENDOR, SUBSCRIPTION, etc.
        
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "UPI"; // UPI, NET_BANKING, CARD, WALLET, etc.
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        // Enhanced metadata for universal payments
        [StringLength(100)]
        public string? MerchantId { get; set; } // For marketplace transactions
        
        [StringLength(100)]
        public string? VendorId { get; set; } // For vendor payments
        
        [StringLength(100)]
        public string? ServiceId { get; set; } // For service bookings
        
        [StringLength(100)]
        public string? SubscriptionId { get; set; } // For subscription payments
        
        // Customer Information
        [StringLength(100)]
        public string? CustomerName { get; set; }
        
        [StringLength(100)]
        public string? CustomerEmail { get; set; }
        
        [StringLength(15)]
        public string? CustomerPhone { get; set; }
        
        // UPI Specific Fields
        [StringLength(100)]
        public string? UpiId { get; set; }
        
        [StringLength(100)]
        public string? PayeeName { get; set; }
        
        [StringLength(50)]
        public string? MerchantCode { get; set; }
        
        [StringLength(500)]
        public string? UpiIntentUrl { get; set; }
        
        // Transaction Details
        [StringLength(100)]
        public string? TransactionId { get; set; } // Bank/UPI transaction ID
        
        [StringLength(100)]
        public string? UpiTransactionId { get; set; } // UPI specific transaction ID
        
        [StringLength(100)]
        public string? BankTransactionId { get; set; } // Bank reference
        
        [StringLength(100)]
        public string? ReferenceNumber { get; set; } // Gateway reference
        
        [StringLength(100)]
        public string? PayerVpa { get; set; } // Payer's VPA
        
        // Error and Failure Details
        [StringLength(50)]
        public string? ErrorCode { get; set; }
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        [StringLength(500)]
        public string? FailureReason { get; set; }
        
        // Webhook and Callback
        [StringLength(500)]
        public string? WebhookUrl { get; set; }
        
        [StringLength(500)]
        public string? SuccessUrl { get; set; }
        
        [StringLength(500)]
        public string? FailureUrl { get; set; }
        
        [StringLength(500)]
        public string? CancelUrl { get; set; }
        
        // Timing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        
        // Metadata - JSON column for flexible data storage
        [Column(TypeName = "json")]
        public string? Metadata { get; set; }
        
        // Audit and Security
        [StringLength(100)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        [StringLength(100)]
        public string? SessionId { get; set; }
        
        // Associated Data
        public int? CreatedShopId { get; set; } // For shop registration payments
        public int? BookingId { get; set; } // For service booking payments
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        [ForeignKey("CreatedShopId")]
        public virtual Shop? CreatedShop { get; set; }
        
        public virtual ICollection<PaymentStatusHistory> StatusHistory { get; set; } = new List<PaymentStatusHistory>();
        public virtual ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
        
        // Audit fields
        public bool IsDeleted { get; set; } = false;
    }

    public class PaymentStatusHistory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PaymentId { get; set; }
        
        [Required]
        public PaymentStatus Status { get; set; }
        
        [Required]
        public PaymentStatus PreviousStatus { get; set; }
        
        [StringLength(500)]
        public string? Remarks { get; set; }
        
        [StringLength(100)]
        public string? UpdatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        [ForeignKey("PaymentId")]
        public virtual Payment Payment { get; set; } = null!;
    }

    public class PaymentRefund
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string RefundId { get; set; } = string.Empty;
        
        [Required]
        public int PaymentId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }
        
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "INR";
        
        [Required]
        public RefundStatus Status { get; set; } = RefundStatus.Created;
        
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? RefundTransactionId { get; set; }
        
        [StringLength(100)]
        public string? BankRefundId { get; set; }
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        [StringLength(100)]
        public string? ProcessedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        
        [Column(TypeName = "json")]
        public string? Metadata { get; set; }
        
        // Navigation Properties
        [ForeignKey("PaymentId")]
        public virtual Payment Payment { get; set; } = null!;
    }

    public class PaymentConfiguration
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ConfigurationKey { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string ConfigurationValue { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public bool IsActive { get; set; } = true;
        
        [Required]
        public bool IsEncrypted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }

    public class PaymentWebhook
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string WebhookId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string PaymentId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty; // payment.success, payment.failed, etc.
        
        [Required]
        [StringLength(20)]
        public string HttpMethod { get; set; } = "POST";
        
        [Required]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty;
        
        [Column(TypeName = "json")]
        public string? Headers { get; set; }
        
        [Column(TypeName = "json")]
        public string? Payload { get; set; }
        
        [Required]
        public WebhookStatus Status { get; set; } = WebhookStatus.Pending;
        
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        [StringLength(10)]
        public string? ResponseCode { get; set; }
        
        [StringLength(1000)]
        public string? ResponseBody { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
    }

    public class PaymentAuditLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string PaymentId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, VERIFY, REFUND, etc.
        
        [StringLength(100)]
        public string? UserId { get; set; }
        
        [StringLength(100)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        [Column(TypeName = "json")]
        public string? OldValues { get; set; }
        
        [Column(TypeName = "json")]
        public string? NewValues { get; set; }
        
        [StringLength(1000)]
        public string? Remarks { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Enums
    public enum PaymentStatus
    {
        Created = 0,
        Pending = 1,
        Processing = 2,
        Success = 3,
        Failed = 4,
        Cancelled = 5,
        Expired = 6,
        Refunded = 7,
        PartiallyRefunded = 8
    }

    public enum RefundStatus
    {
        Created = 0,
        Processing = 1,
        Success = 2,
        Failed = 3,
        Cancelled = 4
    }

    public enum WebhookStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        Retrying = 3,
        Cancelled = 4
    }

    // Universal Payment Types for STIBE App
    public enum StibePaymentType
    {
        // Business Registration
        SHOP_REGISTRATION = 1,
        BUSINESS_LICENSE = 2,
        VENDOR_ONBOARDING = 3,
        
        // Service Payments
        SERVICE_BOOKING = 10,
        APPOINTMENT_FEE = 11,
        CONSULTATION_FEE = 12,
        HOME_SERVICE = 13,
        
        // Marketplace Transactions
        PRODUCT_PURCHASE = 20,
        MARKETPLACE_ORDER = 21,
        BULK_ORDER = 22,
        
        // Vendor & Commission
        VENDOR_PAYMENT = 30,
        COMMISSION_PAYMENT = 31,
        SETTLEMENT_PAYMENT = 32,
        
        // Subscriptions
        MONTHLY_SUBSCRIPTION = 40,
        YEARLY_SUBSCRIPTION = 41,
        PREMIUM_PLAN = 42,
        FEATURE_UNLOCK = 43,
        
        // Platform Fees
        TRANSACTION_FEE = 50,
        PLATFORM_FEE = 51,
        PROCESSING_FEE = 52,
        
        // Advertising & Promotion
        AD_CAMPAIGN = 60,
        BOOST_LISTING = 61,
        FEATURED_PLACEMENT = 62,
        
        // Utilities
        WALLET_TOPUP = 70,
        REFUND_PROCESSING = 71,
        PENALTY_PAYMENT = 72,
        
        // Custom/Other
        CUSTOM_PAYMENT = 99
    }

    public enum PaymentCategory
    {
        BUSINESS = 1,      // Business-related payments
        CONSUMER = 2,      // End-user purchases
        VENDOR = 3,        // Vendor settlements
        SUBSCRIPTION = 4,  // Recurring payments
        PLATFORM = 5,      // Platform fees
        ADVERTISING = 6,   // Marketing & ads
        UTILITY = 7,       // Wallets, refunds
        MARKETPLACE = 8,   // E-commerce transactions
        SERVICE = 9,       // Service bookings
        GENERAL = 10       // Default category
    }

    public enum PaymentMethodType
    {
        UPI = 1,
        NET_BANKING = 2,
        DEBIT_CARD = 3,
        CREDIT_CARD = 4,
        WALLET = 5,
        EMI = 6,
        CASH_ON_DELIVERY = 7,
        BANK_TRANSFER = 8,
        CHEQUE = 9,
        CRYPTO = 10
    }
}
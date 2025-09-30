using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs
{
    // Request DTOs
    public class CreateRazorpayOrderRequestDto
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "INR";
        
        [Required]
        [StringLength(50)]
        public string Purpose { get; set; } = string.Empty; // SHOP_REGISTRATION, SERVICE_BOOKING, etc.
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(40)]
        public string? Receipt { get; set; }
        
        // Shop data for registration payments
        public CreateShopPaymentDataDto? ShopData { get; set; }
        
        // Coupon code for discounts
        [StringLength(50)]
        public string? CouponCode { get; set; }
        
        // Additional metadata
        public Dictionary<string, string> Notes { get; set; } = new();
    }

    public class CreateShopPaymentDataDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(200)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string OpeningTime { get; set; } = "09:00:00";

        [Required]
        public string ClosingTime { get; set; } = "18:00:00";

        public Dictionary<string, object>? BusinessHours { get; set; }

        [StringLength(50)]
        public string? ServiceType { get; set; }

        public List<string>? GenderServices { get; set; }

        public List<string>? Specializations { get; set; }

        [StringLength(20)]
        public string? BankAccountNumber { get; set; }

        [StringLength(11)]
        public string? IFSCCode { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(200)]
        public string? AccountHolderName { get; set; }

        [StringLength(15)]
        public string? GSTNumber { get; set; }

        [StringLength(10)]
        public string? PANNumber { get; set; }

        public decimal? CurrentLatitude { get; set; }
        public decimal? CurrentLongitude { get; set; }

        public List<string>? ImageUrls { get; set; }
        
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }
    }

    public class VerifyRazorpayPaymentRequestDto
    {
        public string? PaymentId { get; set; }
        
        [Required]
        public string RazorpayOrderId { get; set; } = string.Empty;
        
        [Required]
        public string RazorpayPaymentId { get; set; } = string.Empty;
        
        [Required]
        public string RazorpaySignature { get; set; } = string.Empty;
    }

    public class RefundRequestDto
    {
        [Required]
        public string PaymentId { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [StringLength(500)]
        public string? Reason { get; set; }
        
        [StringLength(40)]
        public string? Receipt { get; set; }
        
        public Dictionary<string, string> Notes { get; set; } = new();
    }

    // Response DTOs
    public class RazorpayOrderResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = "CREATED";
        public string Receipt { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public Dictionary<string, string> Notes { get; set; } = new();
        public RazorpayConfigDto RazorpayConfig { get; set; } = new();
    }

    public class RazorpayConfigDto
    {
        public string KeyId { get; set; } = string.Empty;
        public string Name { get; set; } = "STIBE";
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Theme { get; set; } = "#3399cc";
        public bool Modal { get; set; } = true;
        public string Currency { get; set; } = "INR";
    }

    public class PaymentVerificationResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public object? ShopData { get; set; } // Created shop data if payment successful
        public string? FailureReason { get; set; }
        public string? ErrorCode { get; set; }
    }

    public class PaymentStatusResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string? RazorpayPaymentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Purpose { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? Bank { get; set; }
        public string? Wallet { get; set; }
        public string? VPA { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? FailureReason { get; set; }
        public string? ErrorCode { get; set; }
        public decimal RefundedAmount { get; set; }
        public Dictionary<string, string> Notes { get; set; } = new();
    }

    public class RefundResponseDto
    {
        public string RefundId { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = string.Empty;
        public string? Receipt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Reason { get; set; }
        public Dictionary<string, string> Notes { get; set; } = new();
    }

    public class RazorpayWebhookDto
    {
        public string Event { get; set; } = string.Empty;
        public RazorpayWebhookPayloadDto Payload { get; set; } = new();
        public DateTime Created_at { get; set; }
    }

    public class RazorpayWebhookPayloadDto
    {
        public RazorpayPaymentEntityDto Payment { get; set; } = new();
        public RazorpayOrderEntityDto Order { get; set; } = new();
    }

    public class RazorpayPaymentEntityDto
    {
        public string Id { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Order_id { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Bank { get; set; } = string.Empty;
        public string Wallet { get; set; } = string.Empty;
        public string Vpa { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public decimal Tax { get; set; }
        public string Error_code { get; set; } = string.Empty;
        public string Error_description { get; set; } = string.Empty;
        public DateTime Created_at { get; set; }
    }

    public class RazorpayOrderEntityDto
    {
        public string Id { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Receipt { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Created_at { get; set; }
        public Dictionary<string, string> Notes { get; set; } = new();
    }

    // Configuration DTO
    public class PaymentConfigDto
    {
        public decimal ShopRegistrationFee { get; set; } = 500.0m;
        public string Currency { get; set; } = "INR";
        public string CurrencySymbol { get; set; } = "₹";
        public int PaymentTimeoutMinutes { get; set; } = 30;
        public int MaxRetryAttempts { get; set; } = 3;
        public List<string> SupportedPaymentMethods { get; set; } = new()
        {
            "card", "netbanking", "wallet", "upi", "emi"
        };
        public RazorpayConfigDto RazorpayConfig { get; set; } = new();
    }
}
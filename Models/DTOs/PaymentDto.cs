using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs
{
    public class InitiateShopPaymentRequestDto
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; } = 500.0m; // Professional shop registration fee
        
        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "INR";
        
        [Required]
        [StringLength(100)]
        public string Purpose { get; set; } = "SHOP_CREATION";
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        // Shop data to be saved after payment completion
        [Required]
        public CreateShopPaymentDataDto ShopData { get; set; } = new();
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

    public class UpiPaymentResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string UpiIntentUrl { get; set; } = string.Empty;
        public string QrCodeData { get; set; } = string.Empty;
        public string UpiId { get; set; } = string.Empty; // Added separate UPI ID
        public string PayeeName { get; set; } = string.Empty; // Added separate payee name
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } = "PENDING";
    }

    public class VerifyPaymentRequestDto
    {
        [Required]
        [StringLength(100)]
        public string PaymentId { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? TransactionId { get; set; } // UPI transaction ID
        
        [StringLength(50)]
        public string? UpiTransactionRef { get; set; } // UPI reference number
    }

    public class PaymentVerificationResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED, EXPIRED
        public string? TransactionId { get; set; }
        public string? UpiTransactionRef { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? PaymentCompletedAt { get; set; }
        public string? FailureReason { get; set; }
        public object? ShopData { get; set; } // Created shop data if payment successful
    }

    public class PaymentStatusDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Purpose { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? TransactionId { get; set; }
        public string? FailureReason { get; set; }
    }
}
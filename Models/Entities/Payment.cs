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
        public int UserId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "INR";
        
        [Required]
        [StringLength(50)]
        public string Purpose { get; set; } = string.Empty; // SHOP_REGISTRATION, SERVICE_BOOKING, etc.
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "CREATED";
        
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "razorpay";
        
        // Razorpay Specific Fields
        [StringLength(100)]
        public string? RazorpayOrderId { get; set; }
        
        [StringLength(100)]
        public string? RazorpayPaymentId { get; set; }
        
        [StringLength(500)]
        public string? RazorpaySignature { get; set; }
        
        [StringLength(200)]
        public string? Receipt { get; set; }
        
        // Additional Razorpay fields
        [StringLength(100)]
        public string? MethodType { get; set; } // card, netbanking, wallet, upi
        
        [StringLength(100)]
        public string? Bank { get; set; }
        
        [StringLength(100)]
        public string? Wallet { get; set; }
        
        [StringLength(100)]
        public string? VPA { get; set; } // Virtual Payment Address
        
        [Column(TypeName = "text")]
        public string? RazorpayResponseJson { get; set; } // Full Razorpay response
        
        // Shop Data for registration payments
        [Column(TypeName = "text")]
        public string? ShopDataJson { get; set; }
        
        // Error and Failure Details
        [StringLength(500)]
        public string? FailureReason { get; set; }
        
        [StringLength(100)]
        public string? ErrorCode { get; set; }
        
        [StringLength(500)]
        public string? ErrorDescription { get; set; }
        
        // Timing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        
        // Associated Data
        public int? CreatedShopId { get; set; } // For shop registration payments
        
        // Refund Information
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundedAmount { get; set; } = 0;
        
        [StringLength(100)]
        public string? RefundId { get; set; }
        
        public DateTime? RefundedAt { get; set; }
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        [ForeignKey("CreatedShopId")]
        public virtual Shop? CreatedShop { get; set; }
        
        // Audit fields
        public bool IsDeleted { get; set; } = false;
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
}
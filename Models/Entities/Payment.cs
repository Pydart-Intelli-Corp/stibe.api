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
        public string Status { get; set; } = "PENDING";
        
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "UPI";
        
        // UPI Specific Fields
        [StringLength(50)]
        public string? UpiId { get; set; }
        
        [StringLength(100)]
        public string? PayeeName { get; set; }
        
        // Transaction Details
        [StringLength(100)]
        public string? TransactionId { get; set; }
        
        [StringLength(50)]
        public string? UpiTransactionRef { get; set; }
        
        [StringLength(200)]
        public string? TransactionNote { get; set; }
        
        [Column(TypeName = "text")]
        public string? UpiIntentUrl { get; set; }
        
        [Column(TypeName = "text")]
        public string? QrCodeData { get; set; }
        
        // Shop Data for registration payments
        [Column(TypeName = "text")]
        public string? ShopDataJson { get; set; }
        
        // Error and Failure Details
        [StringLength(500)]
        public string? FailureReason { get; set; }
        
        // Timing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        
        // Associated Data
        public int? CreatedShopId { get; set; } // For shop registration payments
        
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
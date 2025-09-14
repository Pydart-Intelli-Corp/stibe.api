using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string Purpose { get; set; } = string.Empty; // SHOP_CREATION, etc.

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED, EXPIRED

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "UPI"; // UPI, CARD, etc.

        // UPI specific fields
        [StringLength(50)]
        public string? UpiId { get; set; } // pa (Payment Address)

        [StringLength(100)]
        public string? PayeeName { get; set; } // pn (Payee Name)

        [StringLength(100)]
        public string? TransactionId { get; set; } // UPI transaction ID

        [StringLength(50)]
        public string? UpiTransactionRef { get; set; } // UPI reference number

        [StringLength(200)]
        public string? TransactionNote { get; set; } // tn (Transaction Note)

        [Column(TypeName = "text")]
        public string? UpiIntentUrl { get; set; } // Complete UPI intent URL

        [Column(TypeName = "text")]
        public string? QrCodeData { get; set; } // QR code data for payment

        // Shop creation data (stored as JSON until payment is completed)
        [Column(TypeName = "text")]
        public string? ShopDataJson { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(15); // 15 minutes expiry

        // Navigation properties
        public int? CreatedShopId { get; set; } // Shop ID if created after successful payment
        
        // Audit fields
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using stibe.api.Models.Entities.PartnersEntity;

namespace stibe.api.Models.Entities
{
    [Table("CouponUsages")]
    public class CouponUsage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string CouponCode { get; set; } = string.Empty;
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Purpose { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalAmount { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Savings { get; set; }
        
        [StringLength(100)]
        public string? PaymentId { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "APPLIED"; // APPLIED, USED, EXPIRED, CANCELLED
        
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
}
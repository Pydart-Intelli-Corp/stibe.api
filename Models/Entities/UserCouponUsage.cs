using stibe.api.Models.Entities.PartnersEntity;
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.Entities
{
    public class UserCouponUsage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string CouponCode { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Purpose { get; set; } = "SHOP_REGISTRATION";
        
        [Required]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsEmailSent { get; set; } = false;
        
        public DateTime? EmailSentAt { get; set; }
        
        public int UsageCount { get; set; } = 0;
        
        public int MaxUsageLimit { get; set; } = 2; // 2 shops per user
        
        public bool IsBlocked { get; set; } = false;
        
        public DateTime? BlockedAt { get; set; }
        
        public string? BlockReason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsDeleted { get; set; } = false;
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}
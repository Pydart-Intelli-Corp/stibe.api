using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs
{
    // Request DTOs
    public class ValidateCouponRequestDto
    {
        [Required]
        [StringLength(50)]
        public string CouponCode { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Purpose { get; set; } = "SHOP_REGISTRATION";
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal OriginalAmount { get; set; }
        
        [EmailAddress]
        public string? UserEmail { get; set; }
        
        public string? PhoneNumber { get; set; }
    }

    public class ApplyCouponRequestDto
    {
        [Required]
        [StringLength(50)]
        public string CouponCode { get; set; } = string.Empty;
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Purpose { get; set; } = "SHOP_REGISTRATION";
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal OriginalAmount { get; set; }
        
        [EmailAddress]
        public string? UserEmail { get; set; }
        
        public string? PhoneNumber { get; set; }
    }

    // Response DTOs
    public class CouponValidationResponseDto
    {
        public bool IsValid { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal DiscountedAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal Savings { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int RemainingUsage { get; set; }
    }

    public class CouponApplicationResponseDto
    {
        public bool Applied { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal Savings { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime AppliedAt { get; set; }
        public int UserId { get; set; }
        public string Purpose { get; set; } = string.Empty;
    }

    // Configuration DTOs
    public class CouponConfigDto
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public bool IsActive { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public int MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
        public List<string> ApplicableFor { get; set; } = new();
    }

    public class CouponSystemConfigDto
    {
        public bool EnableCoupons { get; set; }
        public decimal DefaultDiscountedAmount { get; set; }
        public List<CouponConfigDto> AvailableCoupons { get; set; } = new();
    }
}
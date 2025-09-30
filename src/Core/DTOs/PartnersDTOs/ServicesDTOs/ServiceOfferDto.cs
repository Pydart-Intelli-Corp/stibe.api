// Models/DTOs/PartnersDTOs/ServicesDTOs/ServiceOfferDto.cs
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs
{
    public class ServiceOfferDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public bool IsPercentage { get; set; }
        public string FormattedDiscount => IsPercentage ? $"{DiscountValue}%" : $"₹{DiscountValue}";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string PromoCode { get; set; } = string.Empty;
        public bool RequiresPromoCode { get; set; }
        public int MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public List<int> ApplicableServiceIds { get; set; } = new List<int>();
    }

    public class CreateServiceOfferDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal DiscountValue { get; set; }

        public bool IsPercentage { get; set; } = true;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(200)]
        public string PromoCode { get; set; } = string.Empty;

        public bool RequiresPromoCode { get; set; } = false;

        public int MaxUsage { get; set; } = 0; // 0 means unlimited

        [Required]
        public List<int> ServiceIds { get; set; } = new List<int>();
    }
}

// Models/Entities/PartnersEntity/ServicesEntity/ServiceOffer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity.ServicesEntity
{
    public class ServiceOffer : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }

        public bool IsPercentage { get; set; } = true;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(200)]
        public string PromoCode { get; set; } = string.Empty;

        public bool RequiresPromoCode { get; set; } = false;

        public int MaxUsage { get; set; } = 0; // 0 means unlimited

        public int CurrentUsage { get; set; } = 0;

        public int SalonId { get; set; }

        [ForeignKey("SalonId")]
        public virtual Salon Salon { get; set; } = null!;

        public virtual ICollection<ServiceOfferItem> ServiceOfferItems { get; set; } = new List<ServiceOfferItem>();
    }
}

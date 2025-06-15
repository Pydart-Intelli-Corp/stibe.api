// Models/Entities/PartnersEntity/ServicesEntity/ServiceCategory.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity.ServicesEntity
{
    public class ServiceCategory : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public int SalonId { get; set; }

        [ForeignKey("SalonId")]
        public virtual Salon Salon { get; set; } = null!;

        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
    }
}

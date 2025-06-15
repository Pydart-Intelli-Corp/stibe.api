// Models/Entities/PartnersEntity/ServicesEntity/Service.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity.ServicesEntity
{
    public class Service : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int DurationInMinutes { get; set; } = 30;

        public bool IsActive { get; set; } = true;

        // New fields
        public string ImageUrl { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        public int MaxConcurrentBookings { get; set; } = 1;

        public bool RequiresStaffAssignment { get; set; } = true;

        public int BufferTimeBeforeMinutes { get; set; } = 0;

        public int BufferTimeAfterMinutes { get; set; } = 0;

        // Foreign key
        public int SalonId { get; set; }

        // Navigation properties
        [ForeignKey("SalonId")]
        public virtual Salon Salon { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual ServiceCategory? Category { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public virtual ICollection<ServiceOfferItem> OfferItems { get; set; } = new List<ServiceOfferItem>();

        public virtual ICollection<ServiceAvailability> Availabilities { get; set; } = new List<ServiceAvailability>();
    }
}

using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity
{
    public class Salon : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Longitude { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public TimeSpan OpeningTime { get; set; } = TimeSpan.FromHours(9); // 9:00 AM

        public TimeSpan ClosingTime { get; set; } = TimeSpan.FromHours(18); // 6:00 PM

        public bool IsActive { get; set; } = true;

        // Foreign key
        public int OwnerId { get; set; }

        // Navigation properties
        [ForeignKey("OwnerId")]
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
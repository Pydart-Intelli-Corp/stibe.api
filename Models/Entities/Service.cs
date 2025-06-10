using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities
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

        // Foreign key
        public int SalonId { get; set; }

        // Navigation properties
        [ForeignKey("SalonId")]
        public virtual Salon Salon { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity
{
    public class Shop : BaseEntity
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

        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        public TimeSpan OpeningTime { get; set; } = TimeSpan.FromHours(9); // 9:00 AM

        public TimeSpan ClosingTime { get; set; } = TimeSpan.FromHours(18); // 6:00 PM

        // Business hours as JSON string
        [StringLength(2000)]
        public string? BusinessHours { get; set; }

        // Category and Services properties
        [StringLength(50)]
        public string? ServiceType { get; set; } // shop, beauty_parlour, spa, barbershop, nail_studio, unisex_shop

        [StringLength(500)]
        public string? GenderServices { get; set; } // JSON array of gender services (men, women, kids, unisex)

        [StringLength(1000)]
        public string? Specializations { get; set; } // JSON array of specializations

        // Bank Details
        [StringLength(20)]
        public string? BankAccountNumber { get; set; }

        [StringLength(11)]
        public string? IFSCCode { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(200)]
        public string? AccountHolderName { get; set; }

        // Tax Details
        [StringLength(15)]
        public string? GSTNumber { get; set; }

        [StringLength(10)]
        public string? PANNumber { get; set; }

        public bool IsActive { get; set; } = true;
        
        public bool IsDefault { get; set; } = false;
        
        // Models/Entities/PartnersEntity/Shop.cs - Add these properties
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        [StringLength(4000)]
        public string? ImageUrls { get; set; }


        // Foreign key
        public int OwnerId { get; set; }

        // Navigation properties
        [ForeignKey("OwnerId")]
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
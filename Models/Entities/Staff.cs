using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities
{
    public class Staff : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // "Stylist", "Colorist", "Manager", etc.

        [StringLength(1000)]
        public string Bio { get; set; } = string.Empty;

        [StringLength(500)]
        public string PhotoUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Work Schedule
        public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(9); // 9:00 AM
        public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(17); // 5:00 PM
        public TimeSpan LunchBreakStart { get; set; } = TimeSpan.FromHours(13); // 1:00 PM
        public TimeSpan LunchBreakEnd { get; set; } = TimeSpan.FromHours(14); // 2:00 PM

        // Professional Details
        public int ExperienceYears { get; set; } = 0;
        public decimal EfficiencyMultiplier { get; set; } = 1.0m; // How fast they work (1.0 = normal speed)

        // Financial
        [Column(TypeName = "decimal(10,2)")]
        public decimal HourlyRate { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; } = 40; // Percentage

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string EmploymentType { get; set; } = "Full-Time"; // "Full-Time", "Part-Time", "Contract"

        // Performance tracking
        public decimal AverageRating { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;
        public int TotalServices { get; set; } = 0;

        // Salon relationship
        public int SalonId { get; set; }

        // Additional info
        [StringLength(1000)]
        public string Certifications { get; set; } = string.Empty;

        [StringLength(200)]
        public string Languages { get; set; } = string.Empty;

        [StringLength(100)]
        public string InstagramHandle { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("SalonId")]
        public virtual Salon Salon { get; set; } = null!;

        public virtual ICollection<Booking> AssignedBookings { get; set; } = new List<Booking>();

        public virtual ICollection<StaffSpecialization> Specializations { get; set; } = new List<StaffSpecialization>();

        // Add User relationship
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }


    }
}
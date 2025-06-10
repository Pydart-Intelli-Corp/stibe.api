using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities
{
    public class User : BaseEntity
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
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; } = false;
        // Add these properties to the existing User entity
        public int? SalonId { get; set; } // For staff members
        public bool IsStaffActive { get; set; } = false;
        public DateTime? StaffJoinDate { get; set; }

        // Navigation property for staff profile
        public virtual Staff? StaffProfile { get; set; }

        [ForeignKey("SalonId")]
        public virtual Salon? WorkingSalon { get; set; }
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer"; // Customer, SalonOwner, Admin

        // Navigation properties
        public virtual ICollection<Salon> OwnedSalons { get; set; } = new List<Salon>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
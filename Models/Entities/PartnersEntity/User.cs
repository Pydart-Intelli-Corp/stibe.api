using stibe.api.Models.Entities.PartnersEntity.StaffEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity
{
    public class User : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

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
        public string? ExternalAuthProvider { get; set; }
        public string? ExternalAuthId { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public bool IsEmailVerified { get; set; } = false;
        public DateTime? EmailVerifiedAt { get; set; }
        public string? RegistrationIP { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }
        // Add these properties to the existing User entity
        public int? SalonId { get; set; } // For staff members
        public bool IsStaffActive { get; set; } = false;
        public DateTime? StaffJoinDate { get; set; }

        // Admin-specific properties
        public bool IsSystemAdmin { get; set; } = false;
        public bool CanMonitorSalons { get; set; } = false;
        public bool CanMonitorStaff { get; set; } = false;
        public bool CanMonitorBookings { get; set; } = false;
        public bool CanMonitorUsers { get; set; } = false;
        public bool CanModifySystemSettings { get; set; } = false;
        public DateTime? AdminRoleAssignedDate { get; set; }
        public int? AdminRoleAssignedBy { get; set; }

        // Navigation property for staff profile
        public virtual Staff? StaffProfile { get; set; }

        [ForeignKey("SalonId")]
        public virtual Salon? WorkingSalon { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer"; // Customer, SalonOwner, Admin, SuperAdmin

        // Navigation properties
        public virtual ICollection<Salon> OwnedSalons { get; set; } = new List<Salon>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Method to check if user has specific admin permission
        public bool HasAdminPermission(string permissionType)
        {
            if (Role != "Admin" && Role != "SuperAdmin")
                return false;

            return permissionType switch
            {
                "Salons" => CanMonitorSalons,
                "Staff" => CanMonitorStaff,
                "Bookings" => CanMonitorBookings,
                "Users" => CanMonitorUsers,
                "SystemSettings" => CanModifySystemSettings,
                "All" => IsSystemAdmin,
                _ => false
            };
        }
    }
}

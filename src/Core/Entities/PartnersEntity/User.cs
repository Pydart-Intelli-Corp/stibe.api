using stibe.api.Models.Entities.PartnersEntity.StaffEntity;
using stibe.api.Models.Entities;
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
        public string? ProfilePictureUrl { get; set; }
        public string? GoogleId { get; set; } // For Google OAuth integration

        public bool IsEmailVerified { get; set; } = false;
        public DateTime? EmailVerifiedAt { get; set; }
        public string? RegistrationIP { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }

        // KYC Verification fields
        public bool IsKycVerified { get; set; } = false;
        public string? KycStatus { get; set; } = "NotStarted"; // NotStarted, InProgress, Pending, Verified, Rejected
        public string? AadhaarNumber { get; set; }
        public string? AadhaarImageUrl { get; set; }
        public string? PanNumber { get; set; }
        public string? PanImageUrl { get; set; }
        public DateTime? KycSubmittedAt { get; set; }
        public DateTime? KycVerifiedAt { get; set; }
        public string? KycRejectionReason { get; set; }
        public int? KycVerifiedBy { get; set; } // Admin user ID who verified
        // Add these properties to the existing User entity
        public int? ShopId { get; set; } // For staff members
        public bool IsStaffActive { get; set; } = false;
        public DateTime? StaffJoinDate { get; set; }

        // Admin-specific properties
        public bool IsSystemAdmin { get; set; } = false;
        public bool CanMonitorShops { get; set; } = false;
        public bool CanMonitorStaff { get; set; } = false;
        public bool CanMonitorBookings { get; set; } = false;
        public bool CanMonitorUsers { get; set; } = false;
        public bool CanModifySystemSettings { get; set; } = false;
        public DateTime? AdminRoleAssignedDate { get; set; }
        public int? AdminRoleAssignedBy { get; set; }

        // Navigation property for staff profile
        public virtual Staff? StaffProfile { get; set; }

        [ForeignKey("ShopId")]
        public virtual Shop? WorkingShop { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer"; // Customer, ShopOwner, Admin, SuperAdmin

        // Navigation properties
        public virtual ICollection<Shop> OwnedShops { get; set; } = new List<Shop>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Shop> Shops => OwnedShops; // Alias for consistency
        public virtual KycVerification? KycVerification { get; set; }

        // Method to check if user has specific admin permission
        public bool HasAdminPermission(string permissionType)
        {
            if (Role != "Admin" && Role != "SuperAdmin")
                return false;

            return permissionType switch
            {
                "Shops" => CanMonitorShops,
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

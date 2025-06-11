using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using stibe.api.Models.Entities.PartnersEntity.StaffEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity
{
    public class Booking : BaseEntity
    {
        public DateTime BookingDate { get; set; }

        public TimeSpan BookingTime { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Foreign keys
        public int CustomerId { get; set; }
        public int SalonId { get; set; }
        public int ServiceId { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual User Customer { get; set; } = null!;

        [ForeignKey("SalonId")]
        public virtual Salon Salon { get; set; } = null!;

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;

        public DateTime? ServiceStartedAt { get; set; }
        public DateTime? ServiceCompletedAt { get; set; }

        [StringLength(20)]
        public string ServiceStatus { get; set; } = "Scheduled"; // "Scheduled", "InProgress", "Completed", "NoShow"

        public bool CustomerNotified { get; set; } = false;
        public DateTime? CustomerNotifiedAt { get; set; }

        public bool NextCustomerNotified { get; set; } = false;
        public DateTime? NextCustomerNotifiedAt { get; set; }

        // Staff performance tracking
        public decimal StaffCommissionEarned { get; set; } = 0;
        public decimal CustomerTip { get; set; } = 0;
        public decimal StaffRating { get; set; } = 0;
        // Add these properties to the existing Booking entity
        public int? AssignedStaffId { get; set; }
        public TimeSpan? EstimatedStartTime { get; set; }
        public TimeSpan? EstimatedEndTime { get; set; }
        public int EstimatedDurationMinutes { get; set; }

        [ForeignKey("AssignedStaffId")]
        public virtual Staff? AssignedStaff { get; set; }
        [StringLength(500)]
        public string CustomerFeedback { get; set; } = string.Empty;

    }
}
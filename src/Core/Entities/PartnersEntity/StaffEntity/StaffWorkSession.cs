using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity.StaffEntity
{
    public class StaffWorkSession : BaseEntity
    {
        public int StaffId { get; set; }
        public DateTime WorkDate { get; set; }
        public TimeSpan ClockInTime { get; set; }
        public TimeSpan? ClockOutTime { get; set; }

        public int ScheduledMinutes { get; set; } = 0;
        public int ActualMinutes { get; set; } = 0;
        public int BreakMinutes { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Active"; // "Active", "Completed", "Absent"

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Location tracking (optional)
        [Column(TypeName = "decimal(10,8)")]
        public decimal? ClockInLatitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? ClockInLongitude { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal? ClockOutLatitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? ClockOutLongitude { get; set; }

        // Performance metrics
        public int ServicesCompleted { get; set; } = 0;
        public decimal RevenueGenerated { get; set; } = 0;
        public decimal CommissionEarned { get; set; } = 0;

        // Navigation properties
        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; } = null!;
    }
}
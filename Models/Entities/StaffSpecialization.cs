using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities
{
    public class StaffSpecialization : BaseEntity
    {
        public int StaffId { get; set; }
        public int ServiceId { get; set; }

        [StringLength(20)]
        public string ProficiencyLevel { get; set; } = "Intermediate"; // "Beginner", "Intermediate", "Advanced", "Expert"

        [Column(TypeName = "decimal(3,2)")]
        public decimal ServiceTimeMultiplier { get; set; } = 1.0m; // How much faster/slower than standard time

        public bool IsPreferred { get; set; } = false; // If this staff member prefers this service

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; } = null!;

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;
    }
}
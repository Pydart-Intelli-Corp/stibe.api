using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs
{
    public class StaffLoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class StaffProfileResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Schedule
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan LunchBreakStart { get; set; }
        public TimeSpan LunchBreakEnd { get; set; }

        // Professional
        public int ExperienceYears { get; set; }
        public decimal EfficiencyMultiplier { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal CommissionRate { get; set; }
        public string EmploymentType { get; set; } = string.Empty;

        // Performance
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalServices { get; set; }

        // Salon
        public int SalonId { get; set; }
        public string SalonName { get; set; } = string.Empty;

        // Additional
        public string Certifications { get; set; } = string.Empty;
        public string Languages { get; set; } = string.Empty;
        public string InstagramHandle { get; set; } = string.Empty;

        public DateTime JoinDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Specializations
        public List<StaffSpecializationResponseDto> Specializations { get; set; } = new List<StaffSpecializationResponseDto>();
    }

    public class StaffSpecializationResponseDto
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ProficiencyLevel { get; set; } = string.Empty;
        public decimal ServiceTimeMultiplier { get; set; }
        public bool IsPreferred { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class CreateStaffSpecializationDto
    {
        [Required]
        public int ServiceId { get; set; }

        [Required]
        [StringLength(20)]
        public string ProficiencyLevel { get; set; } = "Intermediate";

        [Range(0.1, 3.0)]
        public decimal ServiceTimeMultiplier { get; set; } = 1.0m;

        public bool IsPreferred { get; set; } = false;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateStaffProfileDto
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        [StringLength(500)]
        public string? PhotoUrl { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? LunchBreakStart { get; set; }
        public TimeSpan? LunchBreakEnd { get; set; }

        [StringLength(1000)]
        public string? Certifications { get; set; }

        [StringLength(200)]
        public string? Languages { get; set; }

        [StringLength(100)]
        public string? InstagramHandle { get; set; }
    }
}
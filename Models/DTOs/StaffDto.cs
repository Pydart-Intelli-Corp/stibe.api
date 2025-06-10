using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs
{
    public class StaffRegistrationRequestDto
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // "Stylist", "Colorist", "Manager"

        [StringLength(1000)]
        public string Bio { get; set; } = string.Empty;

        [Required]
        public int SalonId { get; set; }

        // Work Schedule
        [Required]
        public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(9);

        [Required]
        public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(17);

        public TimeSpan LunchBreakStart { get; set; } = TimeSpan.FromHours(13);
        public TimeSpan LunchBreakEnd { get; set; } = TimeSpan.FromHours(14);

        // Professional Details
        public int ExperienceYears { get; set; } = 0;

        [Range(0.01, 5000.00)]
        public decimal HourlyRate { get; set; } = 0;

        [Range(0, 100)]
        public decimal CommissionRate { get; set; } = 40;

        [StringLength(50)]
        public string EmploymentType { get; set; } = "Full-Time";

        [StringLength(1000)]
        public string Certifications { get; set; } = string.Empty;

        [StringLength(200)]
        public string Languages { get; set; } = string.Empty;

        [StringLength(100)]
        public string InstagramHandle { get; set; } = string.Empty;
    }

    public class StaffResponseDto
    {
        public int Id { get; set; }
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
    }

    public class StaffListResponseDto
    {
        public List<StaffResponseDto> Staff { get; set; } = new List<StaffResponseDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class StaffLoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
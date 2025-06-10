using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs
{
    public class CreateServiceRequestDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000.00)]
        public decimal Price { get; set; }

        [Required]
        [Range(15, 480)] // 15 minutes to 8 hours
        public int DurationInMinutes { get; set; } = 30;
    }

    public class UpdateServiceRequestDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0.01, 10000.00)]
        public decimal? Price { get; set; }

        [Range(15, 480)]
        public int? DurationInMinutes { get; set; }

        public bool? IsActive { get; set; }
    }

    public class ServiceResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInMinutes { get; set; }
        public bool IsActive { get; set; }
        public int SalonId { get; set; }
        public string SalonName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
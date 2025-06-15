// Models/DTOs/PartnersDTOs/ServicesDTOs/ServiceDto.cs
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs
{
    // Update existing request DTOs
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

        // New fields
        public string? ImageUrl { get; set; }

        public int? CategoryId { get; set; }

        public int MaxConcurrentBookings { get; set; } = 1;

        public bool RequiresStaffAssignment { get; set; } = true;

        public int BufferTimeBeforeMinutes { get; set; } = 0;

        public int BufferTimeAfterMinutes { get; set; } = 0;

        // Initial availability settings
        public List<ServiceAvailabilityDto>? Availabilities { get; set; }
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

        // New fields
        public string? ImageUrl { get; set; }

        public int? CategoryId { get; set; }

        public int? MaxConcurrentBookings { get; set; }

        public bool? RequiresStaffAssignment { get; set; }

        public int? BufferTimeBeforeMinutes { get; set; }

        public int? BufferTimeAfterMinutes { get; set; }
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

        // Extended properties
        public string ImageUrl { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int MaxConcurrentBookings { get; set; }
        public bool RequiresStaffAssignment { get; set; }
        public int BufferTimeBeforeMinutes { get; set; }
        public int BufferTimeAfterMinutes { get; set; }

        // Related data
        public List<ServiceAvailabilityDto>? Availabilities { get; set; }
        public List<ServiceOfferDto>? ActiveOffers { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

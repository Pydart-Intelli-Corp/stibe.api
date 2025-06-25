using stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs;
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs
{
    public class CreateSalonRequestDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(200)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string OpeningTime { get; set; } = "09:00:00";

        [Required]
        public string ClosingTime { get; set; } = "18:00:00";

        // Business hours (JSON string)
        public string? BusinessHours { get; set; }

        // Current location properties
        public decimal? CurrentLatitude { get; set; }
        public decimal? CurrentLongitude { get; set; }
        public bool UseCurrentLocation { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public List<IFormFile>? SalonImages { get; set; }
    }


    public class UpdateSalonRequestDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? ZipCode { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }        public string? OpeningTime { get; set; }

        public string? ClosingTime { get; set; }

        public bool? IsActive { get; set; }

        // Location properties
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool? UseCustomLocation { get; set; }
        [StringLength(500)]
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new List<string>();
        public IFormFile? ProfilePictureFile { get; set; }
        public List<IFormFile>? SalonImages { get; set; }

        public IFormFile? ProfilePicture { get; set; }

        public List<string>? ImagesToDelete { get; set; }

    }


    public class SalonResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
        public string? BusinessHours { get; set; }
        public bool IsActive { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public double? DistanceInKm { get; set; } // For location-based searches
        public List<ServiceResponseDto> Services { get; set; } = new List<ServiceResponseDto>();

        public string ProfilePictureUrl { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class SalonSearchRequestDto
    {
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public double? RadiusInKm { get; set; } = 10; // Default 10km radius
        public bool? IsActive { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "distance"; // distance, name, rating
        public string SortOrder { get; set; } = "asc"; // asc, desc
    }

    public class SalonListResponseDto
    {
        public List<SalonResponseDto> Salons { get; set; } = new List<SalonResponseDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // JSON-only version for mobile app (without file uploads)
    public class CreateSalonJsonRequestDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(200)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string OpeningTime { get; set; } = "09:00:00";

        [Required]
        public string ClosingTime { get; set; } = "18:00:00";

        // Business hours as dictionary/object
        public Dictionary<string, BusinessHourDto>? BusinessHours { get; set; }

        // Current location properties
        public decimal? CurrentLatitude { get; set; }
        public decimal? CurrentLongitude { get; set; }
        public bool UseCurrentLocation { get; set; }

        // Image URLs or base64 strings for mobile
        public List<string>? ImageUrls { get; set; }
    }

    public class BusinessHourDto
    {
        public bool IsOpen { get; set; }
        public string Open { get; set; } = "09:00";
        public string Close { get; set; } = "18:00";
    }
}
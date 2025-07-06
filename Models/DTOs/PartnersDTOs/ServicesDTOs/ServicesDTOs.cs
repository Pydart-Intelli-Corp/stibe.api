// Models/DTOs/PartnersDTOs/ServicesDTOs/ServiceDto.cs
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs
{
    // Enhanced request DTOs to match Flutter requirements
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

        // Enhanced fields matching Flutter app
        public string? ImageUrl { get; set; }
        public string? Category { get; set; } // Category name instead of ID for simplicity
        public int? CategoryId { get; set; }
        public decimal? OfferPrice { get; set; } // Special offer pricing
        public string? ProductsUsed { get; set; } // Legacy: Products used in service as string
        public List<ServiceProductDto>? Products { get; set; } // New: Structured products with images
        public List<string>? ServiceImages { get; set; } // Multiple service images
        
        // Existing technical fields
        public int MaxConcurrentBookings { get; set; } = 1;
        public bool RequiresStaffAssignment { get; set; } = true;
        public int BufferTimeBeforeMinutes { get; set; } = 0;
        public int BufferTimeAfterMinutes { get; set; } = 0;

        // Initial availability settings
        public List<ServiceAvailabilityDto>? Availabilities { get; set; }
    }

    public class SuggestCategoryRequestDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class SuggestCategoryResponseDto
    {
        public int? CategoryId { get; set; }
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

        // Enhanced fields matching Flutter app
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public int? CategoryId { get; set; }
        public decimal? OfferPrice { get; set; }
        public string? ProductsUsed { get; set; } // Legacy: Products used in service as string
        public List<ServiceProductDto>? Products { get; set; } // New: Structured products with images
        public List<string>? ServiceImages { get; set; }

        // Existing technical fields
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

        // Enhanced properties matching Flutter app
        public string ImageUrl { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal? OfferPrice { get; set; }
        public string? ProductsUsed { get; set; } // Legacy: Products used in service as string
        public List<ServiceProductDto>? Products { get; set; } // New: Structured products with images
        public List<string>? ServiceImages { get; set; }

        // Technical properties
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

    // Service Category DTOs
    public class CreateServiceCategoryRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public string? IconUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateServiceCategoryRequestDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public string? IconUrl { get; set; }

        public bool? IsActive { get; set; }
    }

    public class ServiceCategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int SalonId { get; set; }
        public bool IsActive { get; set; }
        public int ServiceCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DuplicateServiceRequestDto
    {
        [Required]
        [StringLength(200)]
        public string NewName { get; set; } = string.Empty;
    }

    // Image upload DTOs
    public class ServiceImageUploadDto
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class ServiceImagesUploadDto
    {
        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    // Product DTOs for structured product management
    public class ServiceProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public bool IsUploaded { get; set; } = true;
    }

    public class CreateServiceProductDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class UpdateServiceProductDto
    {
        [StringLength(200)]
        public string? Name { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public List<string>? ImageUrls { get; set; }
    }

    public class ProductImagesUploadDto
    {
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}

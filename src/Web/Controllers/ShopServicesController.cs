using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using stibe.api.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/shop/{shopId}/services")]
    [Authorize(Roles = "ShopOwner")]
    public class ShopServicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShopServicesController> _logger;
        private readonly IFileService _fileService;

        public ShopServicesController(
            ApplicationDbContext context, 
            ILogger<ShopServicesController> logger, 
            IFileService fileService)
        {
            _context = context;
            _logger = logger;
            _fileService = fileService;
        }

        /// <summary>
        /// Get all services for a specific shop
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ServiceResponseDto>>>> GetShopServices(int shopId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<List<ServiceResponseDto>>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<List<ServiceResponseDto>>.ErrorResponse("Shop not found or access denied"));
                }

                var services = await _context.Services
                    .Include(s => s.Category)
                    .Where(s => s.ShopId == shopId && !s.IsDeleted)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var serviceResponses = services.Select(s => new ServiceResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description ?? string.Empty,
                    Price = s.Price,
                    DurationInMinutes = s.DurationInMinutes,
                    IsActive = s.IsActive,
                    ShopId = s.ShopId,
                    ShopName = shop.Name,
                    ImageUrl = s.ImageUrl ?? string.Empty,
                    CategoryId = s.CategoryId,
                    CategoryName = s.Category?.Name ?? string.Empty,
                    OfferPrice = s.OfferPrice,
                    ProductsUsed = s.ProductsUsed ?? string.Empty,
                    ServiceImages = !string.IsNullOrEmpty(s.ServiceImages) 
                        ? JsonSerializer.Deserialize<List<string>>(s.ServiceImages) 
                        : new List<string>(),
                    ProductImages = !string.IsNullOrEmpty(s.ProductImages) 
                        ? JsonSerializer.Deserialize<List<string>>(s.ProductImages) 
                        : new List<string>(),
                    MaxConcurrentBookings = s.MaxConcurrentBookings,
                    RequiresStaffAssignment = s.RequiresStaffAssignment,
                    BufferTimeBeforeMinutes = s.BufferTimeBeforeMinutes,
                    BufferTimeAfterMinutes = s.BufferTimeAfterMinutes,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList();

                return Ok(ApiResponse<List<ServiceResponseDto>>.SuccessResponse(serviceResponses, "Services retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving services for shop {ShopId}", shopId);
                return StatusCode(500, ApiResponse<List<ServiceResponseDto>>.ErrorResponse("An error occurred while retrieving services"));
            }
        }

        /// <summary>
        /// Get services grouped by specialization for a shop
        /// </summary>
        [HttpGet("by-specialization")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, List<ServiceResponseDto>>>>> GetServicesBySpecialization(int shopId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<Dictionary<string, List<ServiceResponseDto>>>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership and get specializations
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<Dictionary<string, List<ServiceResponseDto>>>.ErrorResponse("Shop not found or access denied"));
                }

                var services = await _context.Services
                    .Include(s => s.Category)
                    .Where(s => s.ShopId == shopId && !s.IsDeleted)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                // Parse shop specializations
                var shopSpecializations = new List<string>();
                if (!string.IsNullOrEmpty(shop.Specializations))
                {
                    try
                    {
                        shopSpecializations = JsonSerializer.Deserialize<List<string>>(shop.Specializations) ?? new List<string>();
                    }
                    catch
                    {
                        shopSpecializations = new List<string>();
                    }
                }

                // Group services by category/specialization
                var groupedServices = new Dictionary<string, List<ServiceResponseDto>>();

                // Add specialization groups
                foreach (var specialization in shopSpecializations)
                {
                    groupedServices[specialization] = new List<ServiceResponseDto>();
                }

                // Add uncategorized group for services without category
                if (!groupedServices.ContainsKey("Other Services"))
                {
                    groupedServices["Other Services"] = new List<ServiceResponseDto>();
                }

                foreach (var service in services)
                {
                    var serviceDto = new ServiceResponseDto
                    {
                        Id = service.Id,
                        Name = service.Name,
                        Description = service.Description ?? string.Empty,
                        Price = service.Price,
                        DurationInMinutes = service.DurationInMinutes,
                        IsActive = service.IsActive,
                        ShopId = service.ShopId,
                        ShopName = shop.Name,
                        ImageUrl = service.ImageUrl ?? string.Empty,
                        CategoryId = service.CategoryId,
                        CategoryName = service.Category?.Name ?? string.Empty,
                        OfferPrice = service.OfferPrice,
                        ProductsUsed = service.ProductsUsed ?? string.Empty,
                        ServiceImages = !string.IsNullOrEmpty(service.ServiceImages) 
                            ? JsonSerializer.Deserialize<List<string>>(service.ServiceImages) 
                            : new List<string>(),
                        ProductImages = !string.IsNullOrEmpty(service.ProductImages) 
                            ? JsonSerializer.Deserialize<List<string>>(service.ProductImages) 
                            : new List<string>(),
                        MaxConcurrentBookings = service.MaxConcurrentBookings,
                        RequiresStaffAssignment = service.RequiresStaffAssignment,
                        BufferTimeBeforeMinutes = service.BufferTimeBeforeMinutes,
                        BufferTimeAfterMinutes = service.BufferTimeAfterMinutes,
                        CreatedAt = service.CreatedAt,
                        UpdatedAt = service.UpdatedAt
                    };

                    // Try to match service to specialization
                    var categoryName = service.Category?.Name ?? "";
                    var matchedSpecialization = shopSpecializations.FirstOrDefault(spec => 
                        categoryName.Contains(spec, StringComparison.OrdinalIgnoreCase) ||
                        service.Name.Contains(spec, StringComparison.OrdinalIgnoreCase) ||
                        (service.Description ?? "").Contains(spec, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(matchedSpecialization))
                    {
                        groupedServices[matchedSpecialization].Add(serviceDto);
                    }
                    else
                    {
                        groupedServices["Other Services"].Add(serviceDto);
                    }
                }

                // Remove empty groups
                var result = groupedServices.Where(kvp => kvp.Value.Any()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                return Ok(ApiResponse<Dictionary<string, List<ServiceResponseDto>>>.SuccessResponse(result, "Services grouped by specialization retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving services by specialization for shop {ShopId}", shopId);
                return StatusCode(500, ApiResponse<Dictionary<string, List<ServiceResponseDto>>>.ErrorResponse("An error occurred while retrieving services"));
            }
        }

        /// <summary>
        /// Create a new service for a shop
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ServiceResponseDto>>> CreateService(int shopId, CreateServiceRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Shop not found or access denied"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<ServiceResponseDto>.ErrorResponse("Validation failed", errors));
                }

                // Check for duplicate service name in the same shop
                var existingService = await _context.Services
                    .FirstOrDefaultAsync(s => s.ShopId == shopId && s.Name == request.Name && !s.IsDeleted);

                if (existingService != null)
                {
                    return BadRequest(ApiResponse<ServiceResponseDto>.ErrorResponse($"A service with the name '{request.Name}' already exists in this shop"));
                }

                // Create category if provided and doesn't exist
                ServiceCategory? category = null;
                if (!string.IsNullOrEmpty(request.Category))
                {
                    category = await _context.ServiceCategories
                        .FirstOrDefaultAsync(c => c.ShopId == shopId && c.Name == request.Category && !c.IsDeleted);

                    if (category == null)
                    {
                        category = new ServiceCategory
                        {
                            Name = request.Category,
                            Description = $"Auto-created category for {request.Category}",
                            ShopId = shopId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.ServiceCategories.Add(category);
                        await _context.SaveChangesAsync();
                    }
                }

                var service = new Service
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    DurationInMinutes = request.DurationInMinutes,
                    ShopId = shopId,
                    IsActive = true,
                    ImageUrl = request.ImageUrl ?? string.Empty,
                    CategoryId = category?.Id ?? request.CategoryId,
                    OfferPrice = request.OfferPrice,
                    ProductsUsed = request.ProductsUsed,
                    ServiceImages = request.ServiceImages != null && request.ServiceImages.Any() 
                        ? JsonSerializer.Serialize(request.ServiceImages) 
                        : null,
                    ProductImages = request.ProductImages != null && request.ProductImages.Any() 
                        ? JsonSerializer.Serialize(request.ProductImages) 
                        : null,
                    MaxConcurrentBookings = request.MaxConcurrentBookings,
                    RequiresStaffAssignment = request.RequiresStaffAssignment,
                    BufferTimeBeforeMinutes = request.BufferTimeBeforeMinutes,
                    BufferTimeAfterMinutes = request.BufferTimeAfterMinutes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                // Create availability records if provided
                if (request.Availabilities != null && request.Availabilities.Any())
                {
                    foreach (var availability in request.Availabilities)
                    {
                        var serviceAvailability = new ServiceAvailability
                        {
                            ServiceId = service.Id,
                            DayOfWeek = availability.DayOfWeek,
                            StartTime = availability.StartTime,
                            EndTime = availability.EndTime,
                            IsAvailable = availability.IsAvailable,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.ServiceAvailabilities.Add(serviceAvailability);
                    }
                    await _context.SaveChangesAsync();
                }

                var response = new ServiceResponseDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description ?? string.Empty,
                    Price = service.Price,
                    DurationInMinutes = service.DurationInMinutes,
                    IsActive = service.IsActive,
                    ShopId = service.ShopId,
                    ShopName = shop.Name,
                    ImageUrl = service.ImageUrl ?? string.Empty,
                    CategoryId = service.CategoryId,
                    CategoryName = category?.Name ?? string.Empty,
                    OfferPrice = service.OfferPrice,
                    ProductsUsed = service.ProductsUsed ?? string.Empty,
                    ServiceImages = !string.IsNullOrEmpty(service.ServiceImages) 
                        ? JsonSerializer.Deserialize<List<string>>(service.ServiceImages) 
                        : new List<string>(),
                    MaxConcurrentBookings = service.MaxConcurrentBookings,
                    RequiresStaffAssignment = service.RequiresStaffAssignment,
                    BufferTimeBeforeMinutes = service.BufferTimeBeforeMinutes,
                    BufferTimeAfterMinutes = service.BufferTimeAfterMinutes,
                    CreatedAt = service.CreatedAt,
                    UpdatedAt = service.UpdatedAt
                };

                _logger.LogInformation("Service '{ServiceName}' created successfully for shop {ShopId}", service.Name, shopId);
                return Ok(ApiResponse<ServiceResponseDto>.SuccessResponse(response, "Service created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service for shop {ShopId}", shopId);
                return StatusCode(500, ApiResponse<ServiceResponseDto>.ErrorResponse("An error occurred while creating the service"));
            }
        }

        /// <summary>
        /// Get a specific service by ID
        /// </summary>
        [HttpGet("{serviceId}")]
        public async Task<ActionResult<ApiResponse<ServiceResponseDto>>> GetService(int shopId, int serviceId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Shop not found or access denied"));
                }

                var service = await _context.Services
                    .Include(s => s.Category)
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.ShopId == shopId && !s.IsDeleted);

                if (service == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Service not found"));
                }

                var response = new ServiceResponseDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description ?? string.Empty,
                    Price = service.Price,
                    DurationInMinutes = service.DurationInMinutes,
                    IsActive = service.IsActive,
                    ShopId = service.ShopId,
                    ShopName = shop.Name,
                    ImageUrl = service.ImageUrl ?? string.Empty,
                    CategoryId = service.CategoryId,
                    CategoryName = service.Category?.Name ?? string.Empty,
                    OfferPrice = service.OfferPrice,
                    ProductsUsed = service.ProductsUsed ?? string.Empty,
                    ServiceImages = !string.IsNullOrEmpty(service.ServiceImages) 
                        ? JsonSerializer.Deserialize<List<string>>(service.ServiceImages) 
                        : new List<string>(),
                    MaxConcurrentBookings = service.MaxConcurrentBookings,
                    RequiresStaffAssignment = service.RequiresStaffAssignment,
                    BufferTimeBeforeMinutes = service.BufferTimeBeforeMinutes,
                    BufferTimeAfterMinutes = service.BufferTimeAfterMinutes,
                    CreatedAt = service.CreatedAt,
                    UpdatedAt = service.UpdatedAt
                };

                return Ok(ApiResponse<ServiceResponseDto>.SuccessResponse(response, "Service retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service {ServiceId} for shop {ShopId}", serviceId, shopId);
                return StatusCode(500, ApiResponse<ServiceResponseDto>.ErrorResponse("An error occurred while retrieving the service"));
            }
        }

        /// <summary>
        /// Update a service
        /// </summary>
        [HttpPut("{serviceId}")]
        public async Task<ActionResult<ApiResponse<ServiceResponseDto>>> UpdateService(int shopId, int serviceId, UpdateServiceRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Shop not found or access denied"));
                }

                var service = await _context.Services
                    .Include(s => s.Category)
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.ShopId == shopId && !s.IsDeleted);

                if (service == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Service not found"));
                }

                // Check for duplicate name if name is being changed
                if (!string.IsNullOrEmpty(request.Name) && request.Name != service.Name)
                {
                    var existingService = await _context.Services
                        .FirstOrDefaultAsync(s => s.ShopId == shopId && s.Name == request.Name && s.Id != serviceId && !s.IsDeleted);

                    if (existingService != null)
                    {
                        return BadRequest(ApiResponse<ServiceResponseDto>.ErrorResponse($"A service with the name '{request.Name}' already exists in this shop"));
                    }
                }

                // Update fields
                if (!string.IsNullOrEmpty(request.Name))
                    service.Name = request.Name;
                if (request.Description != null)
                    service.Description = request.Description;
                if (request.Price.HasValue)
                    service.Price = request.Price.Value;
                if (request.DurationInMinutes.HasValue)
                    service.DurationInMinutes = request.DurationInMinutes.Value;
                if (request.IsActive.HasValue)
                    service.IsActive = request.IsActive.Value;
                if (request.ImageUrl != null)
                    service.ImageUrl = request.ImageUrl;
                if (request.OfferPrice.HasValue)
                    service.OfferPrice = request.OfferPrice.Value;
                if (request.ProductsUsed != null)
                    service.ProductsUsed = request.ProductsUsed;
                if (request.ServiceImages != null)
                    service.ServiceImages = request.ServiceImages.Any() ? JsonSerializer.Serialize(request.ServiceImages) : null;
                if (request.ProductImages != null)
                    service.ProductImages = request.ProductImages.Any() ? JsonSerializer.Serialize(request.ProductImages) : null;
                if (request.MaxConcurrentBookings.HasValue)
                    service.MaxConcurrentBookings = request.MaxConcurrentBookings.Value;
                if (request.RequiresStaffAssignment.HasValue)
                    service.RequiresStaffAssignment = request.RequiresStaffAssignment.Value;
                if (request.BufferTimeBeforeMinutes.HasValue)
                    service.BufferTimeBeforeMinutes = request.BufferTimeBeforeMinutes.Value;
                if (request.BufferTimeAfterMinutes.HasValue)
                    service.BufferTimeAfterMinutes = request.BufferTimeAfterMinutes.Value;

                // Handle category update
                if (!string.IsNullOrEmpty(request.Category))
                {
                    var category = await _context.ServiceCategories
                        .FirstOrDefaultAsync(c => c.ShopId == shopId && c.Name == request.Category && !c.IsDeleted);

                    if (category == null)
                    {
                        category = new ServiceCategory
                        {
                            Name = request.Category,
                            Description = $"Auto-created category for {request.Category}",
                            ShopId = shopId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.ServiceCategories.Add(category);
                        await _context.SaveChangesAsync();
                    }
                    service.CategoryId = category.Id;
                }
                else if (request.CategoryId.HasValue)
                {
                    service.CategoryId = request.CategoryId.Value;
                }

                service.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reload category for response
                await _context.Entry(service).Reference(s => s.Category).LoadAsync();

                var response = new ServiceResponseDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description ?? string.Empty,
                    Price = service.Price,
                    DurationInMinutes = service.DurationInMinutes,
                    IsActive = service.IsActive,
                    ShopId = service.ShopId,
                    ShopName = shop.Name,
                    ImageUrl = service.ImageUrl ?? string.Empty,
                    CategoryId = service.CategoryId,
                    CategoryName = service.Category?.Name ?? string.Empty,
                    OfferPrice = service.OfferPrice,
                    ProductsUsed = service.ProductsUsed ?? string.Empty,
                    ServiceImages = !string.IsNullOrEmpty(service.ServiceImages) 
                        ? JsonSerializer.Deserialize<List<string>>(service.ServiceImages) 
                        : new List<string>(),
                    MaxConcurrentBookings = service.MaxConcurrentBookings,
                    RequiresStaffAssignment = service.RequiresStaffAssignment,
                    BufferTimeBeforeMinutes = service.BufferTimeBeforeMinutes,
                    BufferTimeAfterMinutes = service.BufferTimeAfterMinutes,
                    CreatedAt = service.CreatedAt,
                    UpdatedAt = service.UpdatedAt
                };

                _logger.LogInformation("Service {ServiceId} updated successfully for shop {ShopId}", serviceId, shopId);
                return Ok(ApiResponse<ServiceResponseDto>.SuccessResponse(response, "Service updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service {ServiceId} for shop {ShopId}", serviceId, shopId);
                return StatusCode(500, ApiResponse<ServiceResponseDto>.ErrorResponse("An error occurred while updating the service"));
            }
        }

        /// <summary>
        /// Delete a service (soft delete)
        /// </summary>
        [HttpDelete("{serviceId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteService(int shopId, int serviceId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Shop not found or access denied"));
                }

                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.ShopId == shopId && !s.IsDeleted);

                if (service == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Service not found"));
                }

                // Soft delete
                service.IsDeleted = true;
                service.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Service {ServiceId} deleted for shop {ShopId}", serviceId, shopId);
                return Ok(ApiResponse<object>.SuccessResponse(new object(), "Service deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service {ServiceId} for shop {ShopId}", serviceId, shopId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting the service"));
            }
        }

        /// <summary>
        /// Toggle service active status
        /// </summary>
        [HttpPatch("{serviceId}/toggle-active")]
        public async Task<ActionResult<ApiResponse<ServiceResponseDto>>> ToggleServiceActive(int shopId, int serviceId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Shop not found or access denied"));
                }

                var service = await _context.Services
                    .Include(s => s.Category)
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.ShopId == shopId && !s.IsDeleted);

                if (service == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Service not found"));
                }

                service.IsActive = !service.IsActive;
                service.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new ServiceResponseDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description ?? string.Empty,
                    Price = service.Price,
                    DurationInMinutes = service.DurationInMinutes,
                    IsActive = service.IsActive,
                    ShopId = service.ShopId,
                    ShopName = shop.Name,
                    ImageUrl = service.ImageUrl ?? string.Empty,
                    CategoryId = service.CategoryId,
                    CategoryName = service.Category?.Name ?? string.Empty,
                    OfferPrice = service.OfferPrice,
                    ProductsUsed = service.ProductsUsed ?? string.Empty,
                    ServiceImages = !string.IsNullOrEmpty(service.ServiceImages) 
                        ? JsonSerializer.Deserialize<List<string>>(service.ServiceImages) 
                        : new List<string>(),
                    MaxConcurrentBookings = service.MaxConcurrentBookings,
                    RequiresStaffAssignment = service.RequiresStaffAssignment,
                    BufferTimeBeforeMinutes = service.BufferTimeBeforeMinutes,
                    BufferTimeAfterMinutes = service.BufferTimeAfterMinutes,
                    CreatedAt = service.CreatedAt,
                    UpdatedAt = service.UpdatedAt
                };

                var statusText = service.IsActive ? "activated" : "deactivated";
                _logger.LogInformation("Service {ServiceId} {Status} for shop {ShopId}", serviceId, statusText, shopId);
                return Ok(ApiResponse<ServiceResponseDto>.SuccessResponse(response, $"Service {statusText} successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling service status {ServiceId} for shop {ShopId}", serviceId, shopId);
                return StatusCode(500, ApiResponse<ServiceResponseDto>.ErrorResponse("An error occurred while updating the service status"));
            }
        }

        /// <summary>
        /// Upload service images
        /// </summary>
        [HttpPost("{serviceId}/upload-images")]
        public async Task<ActionResult<ApiResponse<ServiceImagesUploadDto>>> UploadServiceImages(int shopId, int serviceId, [FromForm] List<IFormFile> images)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceImagesUploadDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ServiceImagesUploadDto>.ErrorResponse("Shop not found or access denied"));
                }

                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.ShopId == shopId && !s.IsDeleted);

                if (service == null)
                {
                    return NotFound(ApiResponse<ServiceImagesUploadDto>.ErrorResponse("Service not found"));
                }

                if (images == null || !images.Any())
                {
                    return BadRequest(ApiResponse<ServiceImagesUploadDto>.ErrorResponse("No images provided"));
                }

                var uploadedImageUrls = new List<string>();

                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        var fileName = $"service-{serviceId}-{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                        var imageUrl = await _fileService.UploadFileAsync(image, $"service-images/{fileName}");
                        uploadedImageUrls.Add(imageUrl);
                    }
                }

                // Update service with new images
                var existingImages = new List<string>();
                if (!string.IsNullOrEmpty(service.ServiceImages))
                {
                    try
                    {
                        existingImages = JsonSerializer.Deserialize<List<string>>(service.ServiceImages) ?? new List<string>();
                    }
                    catch
                    {
                        existingImages = new List<string>();
                    }
                }

                existingImages.AddRange(uploadedImageUrls);
                service.ServiceImages = JsonSerializer.Serialize(existingImages);
                service.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new ServiceImagesUploadDto
                {
                    ImageUrls = uploadedImageUrls
                };

                _logger.LogInformation("{ImageCount} images uploaded for service {ServiceId} in shop {ShopId}", uploadedImageUrls.Count, serviceId, shopId);
                return Ok(ApiResponse<ServiceImagesUploadDto>.SuccessResponse(response, "Images uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading images for service {ServiceId} in shop {ShopId}", serviceId, shopId);
                return StatusCode(500, ApiResponse<ServiceImagesUploadDto>.ErrorResponse("An error occurred while uploading images"));
            }
        }

        /// <summary>
        /// Upload product images for a service
        /// </summary>
        [HttpPost("{serviceId}/upload-product-images")]
        public async Task<ActionResult<ApiResponse<ProductImagesUploadDto>>> UploadProductImages(int shopId, int serviceId, [FromForm] List<IFormFile> images)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ProductImagesUploadDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ProductImagesUploadDto>.ErrorResponse("Shop not found or access denied"));
                }

                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.ShopId == shopId && !s.IsDeleted);

                if (service == null)
                {
                    return NotFound(ApiResponse<ProductImagesUploadDto>.ErrorResponse("Service not found"));
                }

                if (images == null || !images.Any())
                {
                    return BadRequest(ApiResponse<ProductImagesUploadDto>.ErrorResponse("No images provided"));
                }

                if (images.Count > 6)
                {
                    return BadRequest(ApiResponse<ProductImagesUploadDto>.ErrorResponse("Maximum 6 product images allowed"));
                }

                var uploadedImageUrls = new List<string>();

                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        // Validate image type
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                        var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            return BadRequest(ApiResponse<ProductImagesUploadDto>.ErrorResponse("Only JPG, PNG, and WebP images are allowed"));
                        }

                        // Validate file size (max 5MB)
                        if (image.Length > 5 * 1024 * 1024)
                        {
                            return BadRequest(ApiResponse<ProductImagesUploadDto>.ErrorResponse("Images must be smaller than 5MB"));
                        }

                        var fileName = $"product-{serviceId}-{Guid.NewGuid()}{fileExtension}";
                        var imageUrl = await _fileService.UploadFileAsync(image, $"product-images/{fileName}");
                        uploadedImageUrls.Add(imageUrl);
                    }
                }

                // Update service with new product images
                var existingProductImages = new List<string>();
                if (!string.IsNullOrEmpty(service.ProductImages))
                {
                    try
                    {
                        existingProductImages = JsonSerializer.Deserialize<List<string>>(service.ProductImages) ?? new List<string>();
                    }
                    catch
                    {
                        existingProductImages = new List<string>();
                    }
                }

                existingProductImages.AddRange(uploadedImageUrls);
                
                // Ensure we don't exceed 6 product images
                if (existingProductImages.Count > 6)
                {
                    existingProductImages = existingProductImages.Take(6).ToList();
                }

                service.ProductImages = JsonSerializer.Serialize(existingProductImages);
                service.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new ProductImagesUploadDto
                {
                    ImageUrls = uploadedImageUrls
                };

                _logger.LogInformation("{ImageCount} product images uploaded for service {ServiceId} in shop {ShopId}", uploadedImageUrls.Count, serviceId, shopId);
                return Ok(ApiResponse<ProductImagesUploadDto>.SuccessResponse(response, "Product images uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product images for service {ServiceId} in shop {ShopId}", serviceId, shopId);
                return StatusCode(500, ApiResponse<ProductImagesUploadDto>.ErrorResponse("An error occurred while uploading product images"));
            }
        }

        /// <summary>
        /// Update shop specializations
        /// </summary>
        [HttpPut("specializations")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateShopSpecializations(int shopId, UpdateShopSpecializationsRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Shop not found or access denied"));
                }

                // Update shop specializations
                shop.Specializations = JsonSerializer.Serialize(request.Specializations);
                shop.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Shop {ShopId} specializations updated to: {Specializations}", 
                    shopId, string.Join(", ", request.Specializations));
                return Ok(ApiResponse<object>.SuccessResponse(new object(), "Shop specializations updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating specializations for shop {ShopId}", shopId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while updating shop specializations"));
            }
        }

        /// <summary>
        /// Get specialization statistics for a shop
        /// </summary>
        [HttpGet("specialization-stats")]
        public async Task<ActionResult<ApiResponse<GetShopSpecializationStatsResponseDto>>> GetSpecializationStats(int shopId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<GetShopSpecializationStatsResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<GetShopSpecializationStatsResponseDto>.ErrorResponse("Shop not found or access denied"));
                }

                // Parse shop specializations
                var shopSpecializations = new List<string>();
                if (!string.IsNullOrEmpty(shop.Specializations))
                {
                    try
                    {
                        shopSpecializations = JsonSerializer.Deserialize<List<string>>(shop.Specializations) ?? new List<string>();
                    }
                    catch
                    {
                        shopSpecializations = new List<string>();
                    }
                }

                var services = await _context.Services
                    .Include(s => s.Category)
                    .Include(s => s.Bookings)
                    .Where(s => s.ShopId == shopId && !s.IsDeleted)
                    .ToListAsync();

                var specializationStats = new List<ShopSpecializationStatsDto>();

                foreach (var specialization in shopSpecializations)
                {
                    var specializationServices = services.Where(s => 
                        (s.Category?.Name ?? "").Contains(specialization, StringComparison.OrdinalIgnoreCase) ||
                        s.Name.Contains(specialization, StringComparison.OrdinalIgnoreCase) ||
                        (s.Description ?? "").Contains(specialization, StringComparison.OrdinalIgnoreCase)
                    ).ToList();

                    var totalServices = specializationServices.Count;
                    var activeServices = specializationServices.Count(s => s.IsActive);
                    var averagePrice = totalServices > 0 ? specializationServices.Average(s => s.Price) : 0;
                    var totalBookings = specializationServices.SelectMany(s => s.Bookings).Count();
                    var revenue = specializationServices.SelectMany(s => s.Bookings).Sum(b => b.TotalAmount);

                    specializationStats.Add(new ShopSpecializationStatsDto
                    {
                        Specialization = specialization,
                        TotalServices = totalServices,
                        ActiveServices = activeServices,
                        AveragePrice = averagePrice,
                        TotalBookings = totalBookings,
                        Revenue = revenue
                    });
                }

                var response = new GetShopSpecializationStatsResponseDto
                {
                    SpecializationStats = specializationStats,
                    TotalServices = services.Count,
                    TotalActiveServices = services.Count(s => s.IsActive),
                    TotalRevenue = services.SelectMany(s => s.Bookings).Sum(b => b.TotalAmount),
                    TotalBookings = services.SelectMany(s => s.Bookings).Count()
                };

                return Ok(ApiResponse<GetShopSpecializationStatsResponseDto>.SuccessResponse(response, "Specialization statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving specialization stats for shop {ShopId}", shopId);
                return StatusCode(500, ApiResponse<GetShopSpecializationStatsResponseDto>.ErrorResponse("An error occurred while retrieving specialization statistics"));
            }
        }

        /// <summary>
        /// Get suggested categories based on shop specializations and existing categories
        /// </summary>
        [HttpGet("suggested-categories")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetSuggestedCategories(int shopId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<List<string>>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId.Value && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<List<string>>.ErrorResponse("Shop not found or access denied"));
                }

                var suggestedCategories = new List<string>();

                // 1. Get existing service categories for this shop
                var existingCategories = await _context.ServiceCategories
                    .Where(sc => sc.ShopId == shopId && sc.IsActive && !sc.IsDeleted)
                    .Select(sc => sc.Name)
                    .ToListAsync();

                suggestedCategories.AddRange(existingCategories);

                // 2. Parse and add shop specializations
                var shopSpecializations = new List<string>();
                if (!string.IsNullOrEmpty(shop.Specializations))
                {
                    try
                    {
                        shopSpecializations = JsonSerializer.Deserialize<List<string>>(shop.Specializations) ?? new List<string>();
                        suggestedCategories.AddRange(shopSpecializations);
                    }
                    catch
                    {
                        // Handle parsing error gracefully
                    }
                }

                // 3. Add specialized categories based on shop specializations
                var specializationBasedCategories = new List<string>();
                foreach (var specialization in shopSpecializations)
                {
                    switch (specialization.ToLower().Trim())
                    {
                        case "hair":
                        case "hair care":
                            specializationBasedCategories.AddRange(new[] { "Hair Cut", "Hair Color", "Hair Styling", "Hair Treatment", "Hair Wash" });
                            break;
                        case "facial":
                        case "facial treatments":
                            specializationBasedCategories.AddRange(new[] { "Facial", "Deep Cleansing", "Anti-Aging", "Acne Treatment", "Hydrating Facial" });
                            break;
                        case "massage":
                        case "massage therapy":
                            specializationBasedCategories.AddRange(new[] { "Body Massage", "Head Massage", "Foot Massage", "Deep Tissue", "Swedish Massage" });
                            break;
                        case "nails":
                        case "nail care":
                            specializationBasedCategories.AddRange(new[] { "Manicure", "Pedicure", "Nail Art", "Gel Polish", "Nail Extension" });
                            break;
                        case "waxing":
                            specializationBasedCategories.AddRange(new[] { "Body Waxing", "Facial Waxing", "Eyebrow Shaping", "Upper Lip", "Full Body Wax" });
                            break;
                        case "makeup":
                            specializationBasedCategories.AddRange(new[] { "Bridal Makeup", "Party Makeup", "Professional Makeup", "Eye Makeup", "Makeup Consultation" });
                            break;
                        case "spa":
                            specializationBasedCategories.AddRange(new[] { "Body Treatment", "Aromatherapy", "Hot Stone", "Mud Therapy", "Spa Package" });
                            break;
                        case "skincare":
                            specializationBasedCategories.AddRange(new[] { "Skin Analysis", "Acne Treatment", "Anti-Aging", "Brightening Treatment", "Moisturizing" });
                            break;
                        default:
                            // Add the specialization itself as a category
                            specializationBasedCategories.Add(specialization);
                            break;
                    }
                }

                suggestedCategories.AddRange(specializationBasedCategories);

                // 4. Add default categories based on service type (if no specializations)
                if (!shopSpecializations.Any())
                {
                    var defaultCategories = new List<string>();
                    switch (shop.ServiceType?.ToLower())
                    {
                        case "salon":
                            defaultCategories.AddRange(new[] { "Hair Cut", "Hair Color", "Facial", "Manicure", "Pedicure", "Massage" });
                            break;
                        case "spa":
                            defaultCategories.AddRange(new[] { "Body Massage", "Facial", "Body Treatment", "Aromatherapy", "Hot Stone", "Deep Tissue" });
                            break;
                        case "barbershop":
                            defaultCategories.AddRange(new[] { "Hair Cut", "Beard Trim", "Shave", "Hair Wash", "Styling" });
                            break;
                        case "clinic":
                            defaultCategories.AddRange(new[] { "Consultation", "Treatment", "Therapy", "Checkup", "Procedure" });
                            break;
                        default:
                            defaultCategories.AddRange(new[] { "Basic Service", "Premium Service", "Consultation" });
                            break;
                    }
                    suggestedCategories.AddRange(defaultCategories);
                }

                // 5. Clean up and deduplicate
                var finalCategories = suggestedCategories
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList();

                _logger.LogInformation("Retrieved {CategoryCount} suggested categories for shop {ShopId} with specializations: {Specializations}", 
                    finalCategories.Count, shopId, string.Join(", ", shopSpecializations));

                return Ok(ApiResponse<List<string>>.SuccessResponse(finalCategories, "Suggested categories retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suggested categories for shop {ShopId}", shopId);
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("An error occurred while retrieving suggested categories"));
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
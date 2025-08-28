using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs;
using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using System.Security.Claims;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/shop/{shopId}/service-category")]
    public class ServiceCategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceCategoryController> _logger;

        public ServiceCategoryController(ApplicationDbContext context, ILogger<ServiceCategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ServiceCategoryResponseDto>>> CreateServiceCategory(int shopId, CreateServiceCategoryRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop exists and user owns it
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("Shop not found"));
                }

                if (shop.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only add categories to your own shops");
                }

                // Check if category name already exists for this shop
                var existingCategory = await _context.ServiceCategories
                    .FirstOrDefaultAsync(c => c.ShopId == shopId && c.Name.ToLower() == request.Name.ToLower() && !c.IsDeleted);

                if (existingCategory != null)
                {
                    return BadRequest(ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("Category name already exists"));
                }

                var category = new ServiceCategory
                {
                    Name = request.Name,
                    Description = request.Description,
                    IconUrl = request.IconUrl ?? string.Empty,
                    ShopId = shopId,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ServiceCategories.Add(category);
                await _context.SaveChangesAsync();

                var response = MapToCategoryResponse(category);

                _logger.LogInformation($"Service category created successfully: {category.Name} for shop {shopId} by user {currentUserId}");
                return Ok(ApiResponse<ServiceCategoryResponseDto>.SuccessResponse(response, "Service category created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service category");
                return StatusCode(500, ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("An error occurred while creating the service category"));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ServiceCategoryResponseDto>>>> GetServiceCategories(
            int shopId,
            bool includeInactive = false,
            bool includeServiceCount = false)
        {
            try
            {
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<List<ServiceCategoryResponseDto>>.ErrorResponse("Shop not found"));
                }

                var query = _context.ServiceCategories.AsQueryable()
                    .Where(c => c.ShopId == shopId && !c.IsDeleted);

                if (!includeInactive)
                {
                    query = query.Where(c => c.IsActive);
                }

                if (includeServiceCount)
                {
                    query = query.Include(c => c.Services.Where(s => !s.IsDeleted));
                }

                var categories = await query
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var response = categories.Select(c => MapToCategoryResponse(c, includeServiceCount)).ToList();

                return Ok(ApiResponse<List<ServiceCategoryResponseDto>>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service categories");
                return StatusCode(500, ApiResponse<List<ServiceCategoryResponseDto>>.ErrorResponse("An error occurred while retrieving service categories"));
            }
        }

        [HttpGet("{categoryId}")]
        public async Task<ActionResult<ApiResponse<ServiceCategoryResponseDto>>> GetServiceCategory(
            int shopId,
            int categoryId,
            bool includeServices = false)
        {
            try
            {
                var query = _context.ServiceCategories.AsQueryable()
                    .Where(c => c.Id == categoryId && c.ShopId == shopId && !c.IsDeleted);

                if (includeServices)
                {
                    query = query.Include(c => c.Services.Where(s => !s.IsDeleted));
                }

                var category = await query.FirstOrDefaultAsync();

                if (category == null)
                {
                    return NotFound(ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("Service category not found"));
                }

                var response = MapToCategoryResponse(category, includeServices);

                return Ok(ApiResponse<ServiceCategoryResponseDto>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service category");
                return StatusCode(500, ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("An error occurred while retrieving the service category"));
            }
        }

        [HttpPut("{categoryId}")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ServiceCategoryResponseDto>>> UpdateServiceCategory(
            int shopId,
            int categoryId,
            UpdateServiceCategoryRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("Shop not found"));
                }

                if (shop.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only update categories for your own shops");
                }

                var category = await _context.ServiceCategories
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.ShopId == shopId && !c.IsDeleted);

                if (category == null)
                {
                    return NotFound(ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("Service category not found"));
                }

                // Check if new name conflicts with existing categories
                if (!string.IsNullOrEmpty(request.Name) && request.Name != category.Name)
                {
                    var existingCategory = await _context.ServiceCategories
                        .FirstOrDefaultAsync(c => c.ShopId == shopId && c.Name.ToLower() == request.Name.ToLower() && c.Id != categoryId && !c.IsDeleted);

                    if (existingCategory != null)
                    {
                        return BadRequest(ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("Category name already exists"));
                    }
                }

                // Update fields
                if (!string.IsNullOrEmpty(request.Name)) category.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Description)) category.Description = request.Description;
                if (request.IconUrl != null) category.IconUrl = request.IconUrl;
                if (request.IsActive.HasValue) category.IsActive = request.IsActive.Value;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = MapToCategoryResponse(category);

                _logger.LogInformation($"Service category updated successfully: {category.Name} for shop {shopId} by user {currentUserId}");
                return Ok(ApiResponse<ServiceCategoryResponseDto>.SuccessResponse(response, "Service category updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service category");
                return StatusCode(500, ApiResponse<ServiceCategoryResponseDto>.ErrorResponse("An error occurred while updating the service category"));
            }
        }

        [HttpDelete("{categoryId}")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse>> DeleteServiceCategory(int shopId, int categoryId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
                }

                // Verify shop ownership
                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse.ErrorResponse("Shop not found"));
                }

                if (shop.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only delete categories from your own shops");
                }

                var category = await _context.ServiceCategories
                    .Include(c => c.Services)
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.ShopId == shopId && !c.IsDeleted);

                if (category == null)
                {
                    return NotFound(ApiResponse.ErrorResponse("Service category not found"));
                }

                // Check if category has services
                var activeServices = category.Services.Where(s => !s.IsDeleted && s.IsActive).ToList();
                if (activeServices.Any())
                {
                    return BadRequest(ApiResponse.ErrorResponse($"Cannot delete category with {activeServices.Count} active services. Please move or delete the services first."));
                }

                // Soft delete the category
                category.IsDeleted = true;
                category.UpdatedAt = DateTime.UtcNow;

                // Remove category reference from services
                var servicesInCategory = category.Services.Where(s => !s.IsDeleted).ToList();
                foreach (var service in servicesInCategory)
                {
                    service.CategoryId = null;
                    service.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Service category deleted successfully: {category.Name} for shop {shopId} by user {currentUserId}");
                return Ok(ApiResponse.SuccessResponse("Service category deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service category");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deleting the service category"));
            }
        }

        private ServiceCategoryResponseDto MapToCategoryResponse(ServiceCategory category, bool includeServiceCount = false)
        {
            return new ServiceCategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IconUrl = category.IconUrl,
                ShopId = category.ShopId,
                IsActive = category.IsActive,
                ServiceCount = includeServiceCount ? category.Services?.Count(s => !s.IsDeleted) ?? 0 : 0,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}

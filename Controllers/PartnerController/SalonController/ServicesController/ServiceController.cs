using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs;
using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using System.Security.Claims;

namespace stibe.api.Controllers.PartnerController.ServicesController
{
    [ApiController]
    [Route("api/salon/{salonId}/[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceController> _logger;

        public ServiceController(ApplicationDbContext context, ILogger<ServiceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<ServiceResponseDto>>> CreateService(int salonId, CreateServiceRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify salon exists and user owns it
                var salon = await _context.Salons
                    .FirstOrDefaultAsync(s => s.Id == salonId && !s.IsDeleted);

                if (salon == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Salon not found"));
                }

                if (salon.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only add services to your own salons");
                }

                var service = new Service
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    DurationInMinutes = request.DurationInMinutes,
                    SalonId = salonId,
                    IsActive = true
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                // Load the service with salon information
                var createdService = await _context.Services
                    .Include(s => s.Salon)
                    .FirstOrDefaultAsync(s => s.Id == service.Id);

                var response = MapToServiceResponse(createdService!);

                _logger.LogInformation($"Service created successfully: {service.Name} for salon {salonId} by user {currentUserId}");
                return Ok(ApiResponse<ServiceResponseDto>.SuccessResponse(response, "Service created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service");
                return StatusCode(500, ApiResponse<ServiceResponseDto>.ErrorResponse("An error occurred while creating the service"));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ServiceResponseDto>>>> GetServices(int salonId, bool includeInactive = false)
        {
            try
            {
                var salon = await _context.Salons
                    .FirstOrDefaultAsync(s => s.Id == salonId && !s.IsDeleted);

                if (salon == null)
                {
                    return NotFound(ApiResponse<List<ServiceResponseDto>>.ErrorResponse("Salon not found"));
                }

                var query = _context.Services
                    .Include(s => s.Salon)
                    .Where(s => s.SalonId == salonId);

                if (!includeInactive)
                {
                    query = query.Where(s => s.IsActive);
                }

                var services = await query.OrderBy(s => s.Name).ToListAsync();
                var response = services.Select(MapToServiceResponse).ToList();

                return Ok(ApiResponse<List<ServiceResponseDto>>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services");
                return StatusCode(500, ApiResponse<List<ServiceResponseDto>>.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("{serviceId}")]
        public async Task<ActionResult<ApiResponse<ServiceResponseDto>>> GetService(int salonId, int serviceId)
        {
            try
            {
                var service = await _context.Services
                    .Include(s => s.Salon)
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.SalonId == salonId);

                if (service == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Service not found"));
                }

                var response = MapToServiceResponse(service);
                return Ok(ApiResponse<ServiceResponseDto>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service");
                return StatusCode(500, ApiResponse<ServiceResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpPut("{serviceId}")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<ServiceResponseDto>>> UpdateService(int salonId, int serviceId, UpdateServiceRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ServiceResponseDto>.ErrorResponse("Invalid token"));
                }

                var service = await _context.Services
                    .Include(s => s.Salon)
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.SalonId == salonId);

                if (service == null)
                {
                    return NotFound(ApiResponse<ServiceResponseDto>.ErrorResponse("Service not found"));
                }

                // Check if the current user owns the salon
                if (service.Salon.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only update services in your own salons");
                }

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(request.Name))
                    service.Name = request.Name;

                if (request.Description != null)
                    service.Description = request.Description;

                if (request.Price.HasValue)
                    service.Price = request.Price.Value;

                if (request.DurationInMinutes.HasValue)
                    service.DurationInMinutes = request.DurationInMinutes.Value;

                if (request.IsActive.HasValue)
                    service.IsActive = request.IsActive.Value;

                await _context.SaveChangesAsync();

                var response = MapToServiceResponse(service);

                _logger.LogInformation($"Service updated successfully: {service.Name} by user {currentUserId}");
                return Ok(ApiResponse<ServiceResponseDto>.SuccessResponse(response, "Service updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service");
                return StatusCode(500, ApiResponse<ServiceResponseDto>.ErrorResponse("An error occurred while updating the service"));
            }
        }

        [HttpDelete("{serviceId}")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse>> DeleteService(int salonId, int serviceId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
                }

                var service = await _context.Services
                    .Include(s => s.Salon)
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.SalonId == salonId);

                if (service == null)
                {
                    return NotFound(ApiResponse.ErrorResponse("Service not found"));
                }

                // Check if the current user owns the salon
                if (service.Salon.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only delete services from your own salons");
                }

                // Check if service has any bookings
                var hasBookings = await _context.Bookings
                    .AnyAsync(b => b.ServiceId == serviceId);

                if (hasBookings)
                {
                    // Soft delete if there are bookings
                    service.IsActive = false;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Service deactivated: {service.Name} by user {currentUserId}");
                    return Ok(ApiResponse.SuccessResponse("Service deactivated successfully (has existing bookings)"));
                }
                else
                {
                    // Hard delete if no bookings
                    _context.Services.Remove(service);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Service deleted: {service.Name} by user {currentUserId}");
                    return Ok(ApiResponse.SuccessResponse("Service deleted successfully"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deleting the service"));
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        private ServiceResponseDto MapToServiceResponse(Service service)
        {
            return new ServiceResponseDto
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                DurationInMinutes = service.DurationInMinutes,
                IsActive = service.IsActive,
                SalonId = service.SalonId,
                SalonName = service.Salon.Name,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt
            };
        }
    }
}
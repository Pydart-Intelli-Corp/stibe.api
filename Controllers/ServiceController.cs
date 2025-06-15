using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using stibe.api.Models.Entities.PartnersEntity.StaffEntity;
using System.Security.Claims;

namespace stibe.api.Controllers
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

                // Check category if provided
                if (request.CategoryId.HasValue)
                {
                    var categoryExists = await _context.ServiceCategories
                        .AnyAsync(c => c.Id == request.CategoryId.Value && c.SalonId == salonId);

                    if (!categoryExists)
                    {
                        return BadRequest(ApiResponse<ServiceResponseDto>.ErrorResponse("Invalid category"));
                    }
                }

                var service = new Service
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    DurationInMinutes = request.DurationInMinutes,
                    SalonId = salonId,
                    IsActive = true,
                    // New fields
                    ImageUrl = request.ImageUrl ?? string.Empty,
                    CategoryId = request.CategoryId,
                    MaxConcurrentBookings = request.MaxConcurrentBookings,
                    RequiresStaffAssignment = request.RequiresStaffAssignment,
                    BufferTimeBeforeMinutes = request.BufferTimeBeforeMinutes,
                    BufferTimeAfterMinutes = request.BufferTimeAfterMinutes
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                // Add service availability if provided
                if (request.Availabilities != null && request.Availabilities.Any())
                {
                    var availabilities = request.Availabilities.Select(a => new ServiceAvailability
                    {
                        ServiceId = service.Id,
                        DayOfWeek = a.DayOfWeek,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        IsAvailable = a.IsAvailable,
                        MaxBookingsPerSlot = a.MaxBookingsPerSlot,
                        SlotDurationMinutes = a.SlotDurationMinutes,
                        BufferTimeMinutes = a.BufferTimeMinutes
                    }).ToList();

                    _context.ServiceAvailabilities.AddRange(availabilities);
                    await _context.SaveChangesAsync();
                }

                // Load the service with salon information and related data
                var createdService = await _context.Services
                    .Include(s => s.Salon)
                    .Include(s => s.Category)
                    .Include(s => s.Availabilities)
                    .Include(s => s.OfferItems)
                        .ThenInclude(oi => oi.Offer)
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
        public async Task<ActionResult<ApiResponse<List<ServiceResponseDto>>>> GetServices(
            int salonId,
            bool includeInactive = false,
            int? categoryId = null,
            bool includeAvailability = false,
            bool includeOffers = false)
        {
            try
            {
                var salon = await _context.Salons
                    .FirstOrDefaultAsync(s => s.Id == salonId && !s.IsDeleted);

                if (salon == null)
                {
                    return NotFound(ApiResponse<List<ServiceResponseDto>>.ErrorResponse("Salon not found"));
                }

                // Start with the base query
                var query = _context.Services.AsQueryable();

                // Add includes one by one
                query = query.Include(s => s.Salon);
                query = query.Include(s => s.Category);

                if (includeAvailability)
                {
                    query = query.Include(s => s.Availabilities);
                }

                if (includeOffers)
                {
                    query = query.Include(s => s.OfferItems)
                        .ThenInclude(oi => oi.Offer);
                }

                // Apply filters
                query = query.Where(s => s.SalonId == salonId);

                if (!includeInactive)
                {
                    query = query.Where(s => s.IsActive);
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(s => s.CategoryId == categoryId.Value);
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
        public async Task<ActionResult<ApiResponse<ServiceResponseDto>>> GetService(
            int salonId,
            int serviceId,
            bool includeAvailability = false,
            bool includeOffers = false)
        {
            try
            {
                // Start with base query
                var query = _context.Services.AsQueryable();

                // Add includes one by one
                query = query.Include(s => s.Salon);
                query = query.Include(s => s.Category);

                if (includeAvailability)
                {
                    query = query.Include(s => s.Availabilities);
                }

                if (includeOffers)
                {
                    query = query.Include(s => s.OfferItems)
                        .ThenInclude(oi => oi.Offer);
                }

                var service = await query
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
                    .Include(s => s.Category)
                    .Include(s => s.OfferItems)
                        .ThenInclude(oi => oi.Offer)
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

                // Update new fields
                if (request.ImageUrl != null)
                    service.ImageUrl = request.ImageUrl;

                if (request.CategoryId.HasValue)
                {
                    var categoryExists = await _context.ServiceCategories
                        .AnyAsync(c => c.Id == request.CategoryId.Value && c.SalonId == salonId);

                    if (!categoryExists)
                    {
                        return BadRequest(ApiResponse<ServiceResponseDto>.ErrorResponse("Invalid category"));
                    }
                    service.CategoryId = request.CategoryId;
                }

                if (request.MaxConcurrentBookings.HasValue)
                    service.MaxConcurrentBookings = request.MaxConcurrentBookings.Value;

                if (request.RequiresStaffAssignment.HasValue)
                    service.RequiresStaffAssignment = request.RequiresStaffAssignment.Value;

                if (request.BufferTimeBeforeMinutes.HasValue)
                    service.BufferTimeBeforeMinutes = request.BufferTimeBeforeMinutes.Value;

                if (request.BufferTimeAfterMinutes.HasValue)
                    service.BufferTimeAfterMinutes = request.BufferTimeAfterMinutes.Value;

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

                // Check if service is part of any offers
                var hasOffers = await _context.ServiceOfferItems
                    .AnyAsync(oi => oi.ServiceId == serviceId);

                if (hasBookings || hasOffers)
                {
                    // Soft delete if there are bookings or offers
                    service.IsActive = false;
                    await _context.SaveChangesAsync();

                    var reason = hasBookings ? "has existing bookings" : "is part of active offers";
                    _logger.LogInformation($"Service deactivated: {service.Name} by user {currentUserId} ({reason})");
                    return Ok(ApiResponse.SuccessResponse($"Service deactivated successfully ({reason})"));
                }
                else
                {
                    // Delete any availability records first
                    var availabilities = await _context.ServiceAvailabilities
                        .Where(a => a.ServiceId == serviceId)
                        .ToListAsync();

                    if (availabilities.Any())
                    {
                        _context.ServiceAvailabilities.RemoveRange(availabilities);
                        await _context.SaveChangesAsync();
                    }

                    // Hard delete if no bookings or offers
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

        [HttpGet("popular")]
        public async Task<ActionResult<ApiResponse<List<ServiceResponseDto>>>> GetPopularServices(int salonId, int count = 5)
        {
            try
            {
                var salon = await _context.Salons
                    .FirstOrDefaultAsync(s => s.Id == salonId && !s.IsDeleted);

                if (salon == null)
                {
                    return NotFound(ApiResponse<List<ServiceResponseDto>>.ErrorResponse("Salon not found"));
                }

                // Get active services with booking counts
                var popularServices = await _context.Services
                    .Include(s => s.Salon)
                    .Include(s => s.Category)
                    .Include(s => s.Bookings)
                    .Include(s => s.OfferItems)
                        .ThenInclude(oi => oi.Offer)
                    .Where(s => s.SalonId == salonId && s.IsActive)
                    .OrderByDescending(s => s.Bookings.Count)
                    .Take(count)
                    .ToListAsync();

                var response = popularServices.Select(MapToServiceResponse).ToList();

                return Ok(ApiResponse<List<ServiceResponseDto>>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular services");
                return StatusCode(500, ApiResponse<List<ServiceResponseDto>>.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("with-offers")]
        public async Task<ActionResult<ApiResponse<List<ServiceResponseDto>>>> GetServicesWithOffers(int salonId)
        {
            try
            {
                var now = DateTime.Now;

                var servicesWithOffers = await _context.ServiceOfferItems
                    .Include(oi => oi.Service)
                        .ThenInclude(s => s.Salon)
                    .Include(oi => oi.Service)
                        .ThenInclude(s => s.Category)
                    .Include(oi => oi.Offer)
                    .Where(oi =>
                        oi.Service.SalonId == salonId &&
                        oi.Service.IsActive &&
                        oi.Offer.IsActive &&
                        oi.Offer.StartDate <= now &&
                        oi.Offer.EndDate >= now)
                    .Select(oi => oi.Service)
                    .Distinct()
                    .ToListAsync();

                var response = servicesWithOffers.Select(MapToServiceResponse).ToList();

                return Ok(ApiResponse<List<ServiceResponseDto>>.SuccessResponse(response,
                    $"Found {response.Count} services with active offers"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services with offers");
                return StatusCode(500, ApiResponse<List<ServiceResponseDto>>.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("by-staff/{staffId}")]
        public async Task<ActionResult<ApiResponse<List<ServiceResponseDto>>>> GetServicesByStaff(int salonId, int staffId)
        {
            try
            {
                // First verify the staff member exists and belongs to this salon
                var staffExists = await _context.Staff
                    .AnyAsync(s => s.Id == staffId && s.SalonId == salonId && s.IsActive);

                if (!staffExists)
                {
                    return NotFound(ApiResponse<List<ServiceResponseDto>>.ErrorResponse("Staff member not found"));
                }

                // Get services through staff specializations
                var services = await _context.StaffSpecializations
                    .Include(ss => ss.Service)
                        .ThenInclude(s => s.Salon)
                    .Include(ss => ss.Service)
                        .ThenInclude(s => s.Category)
                    .Include(ss => ss.Service)
                        .ThenInclude(s => s.OfferItems)
                            .ThenInclude(oi => oi.Offer)
                    .Where(ss => ss.StaffId == staffId && ss.Service.IsActive)
                    .Select(ss => ss.Service)
                    .ToListAsync();

                var response = services.Select(MapToServiceResponse).ToList();

                return Ok(ApiResponse<List<ServiceResponseDto>>.SuccessResponse(response,
                    $"Found {response.Count} services offered by this staff member"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by staff");
                return StatusCode(500, ApiResponse<List<ServiceResponseDto>>.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<ServiceResponseDto>>>> SearchServices(
            int salonId,
            string? query = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minDuration = null,
            int? maxDuration = null,
            int? categoryId = null)
        {
            try
            {
                var salon = await _context.Salons
                    .FirstOrDefaultAsync(s => s.Id == salonId && !s.IsDeleted);

                if (salon == null)
                {
                    return NotFound(ApiResponse<List<ServiceResponseDto>>.ErrorResponse("Salon not found"));
                }

                // Start with base query for active services
                var serviceQuery = _context.Services
                    .Include(s => s.Salon)
                    .Include(s => s.Category)
                    .Include(s => s.OfferItems)
                        .ThenInclude(oi => oi.Offer)
                    .Where(s => s.SalonId == salonId && s.IsActive);

                // Apply search filters if provided
                if (!string.IsNullOrWhiteSpace(query))
                {
                    query = query.ToLower();
                    serviceQuery = serviceQuery.Where(s =>
                        s.Name.ToLower().Contains(query) ||
                        (s.Description != null && s.Description.ToLower().Contains(query)));
                }

                if (minPrice.HasValue)
                {
                    serviceQuery = serviceQuery.Where(s => s.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    serviceQuery = serviceQuery.Where(s => s.Price <= maxPrice.Value);
                }

                if (minDuration.HasValue)
                {
                    serviceQuery = serviceQuery.Where(s => s.DurationInMinutes >= minDuration.Value);
                }

                if (maxDuration.HasValue)
                {
                    serviceQuery = serviceQuery.Where(s => s.DurationInMinutes <= maxDuration.Value);
                }

                if (categoryId.HasValue)
                {
                    serviceQuery = serviceQuery.Where(s => s.CategoryId == categoryId);
                }

                // Execute query and map to DTOs
                var services = await serviceQuery.ToListAsync();
                var response = services.Select(MapToServiceResponse).ToList();

                return Ok(ApiResponse<List<ServiceResponseDto>>.SuccessResponse(response,
                    $"Found {response.Count} services matching your criteria"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching services");
                return StatusCode(500, ApiResponse<List<ServiceResponseDto>>.ErrorResponse("An error occurred while searching services"));
            }
        }

        [HttpPost("{serviceId}/assign-staff")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse>> AssignStaffToService(int salonId, int serviceId, [FromBody] List<int> staffIds)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
                }

                // Verify service exists and belongs to salon owned by user
                var service = await _context.Services
                    .Include(s => s.Salon)
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.SalonId == salonId);

                if (service == null)
                {
                    return NotFound(ApiResponse.ErrorResponse("Service not found"));
                }

                if (service.Salon.OwnerId != currentUserId)
                {
                    return Forbid("You can only assign staff to services in your own salons");
                }

                // Validate staff members exist and belong to this salon
                var validStaffIds = await _context.Staff
                    .Where(s => staffIds.Contains(s.Id) && s.SalonId == salonId && s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (validStaffIds.Count != staffIds.Count)
                {
                    return BadRequest(ApiResponse.ErrorResponse("One or more staff members are invalid or don't belong to this salon"));
                }

                // Get existing specializations
                var existingSpecializations = await _context.StaffSpecializations
                    .Where(ss => ss.ServiceId == serviceId)
                    .ToListAsync();

                // Create a set of existing staff IDs
                var existingStaffIds = existingSpecializations.Select(ss => ss.StaffId).ToHashSet();

                // Create a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Add new specializations
                    foreach (var staffId in validStaffIds)
                    {
                        if (!existingStaffIds.Contains(staffId))
                        {
                            _context.StaffSpecializations.Add(new StaffSpecialization
                            {
                                StaffId = staffId,
                                ServiceId = serviceId
                            });
                        }
                    }

                    // Remove specializations that are no longer needed
                    foreach (var specialization in existingSpecializations)
                    {
                        if (!validStaffIds.Contains(specialization.StaffId))
                        {
                            _context.StaffSpecializations.Remove(specialization);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Successfully assigned {validStaffIds.Count} staff members to service {serviceId}");
                    return Ok(ApiResponse.SuccessResponse("Staff assigned successfully"));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning staff to service");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while assigning staff"));
            }
        }

        [HttpGet("availability/{serviceId}/{date}")]
        public async Task<ActionResult<ApiResponse<List<TimeSlotDto>>>> GetAvailableTimeSlots(
            int salonId,
            int serviceId,
            DateTime date,
            int? staffId = null)
        {
            try
            {
                // Validate service exists
                var service = await _context.Services
                    .Include(s => s.Availabilities)
                    .FirstOrDefaultAsync(s => s.Id == serviceId && s.SalonId == salonId && s.IsActive);

                if (service == null)
                {
                    return NotFound(ApiResponse<List<TimeSlotDto>>.ErrorResponse("Service not found"));
                }

                // Get service availability for the day of the week
                var dayOfWeek = date.DayOfWeek.ToString();
                var serviceAvailability = service.Availabilities
                    .FirstOrDefault(a => a.DayOfWeek.ToString() == dayOfWeek && a.IsAvailable);

                if (serviceAvailability == null)
                {
                    return Ok(ApiResponse<List<TimeSlotDto>>.SuccessResponse(new List<TimeSlotDto>(),
                        "No availability for this service on the selected day"));
                }

                // Check staff assignments
                if (service.RequiresStaffAssignment && staffId.HasValue)
                {
                    // Verify staff member is assigned to this service
                    var isStaffAssigned = await _context.StaffSpecializations
                        .AnyAsync(ss => ss.ServiceId == serviceId && ss.StaffId == staffId.Value);

                    if (!isStaffAssigned)
                    {
                        return BadRequest(ApiResponse<List<TimeSlotDto>>.ErrorResponse("Selected staff member is not assigned to this service"));
                    }
                }

                // Get existing bookings for this service on the specified date
                var existingBookings = await _context.Bookings
                    .Where(b =>
                        b.ServiceId == serviceId &&
                        b.BookingDate.Date == date.Date &&
                        (staffId == null || b.AssignedStaffId == staffId.Value) &&
                        b.Status != "Cancelled")
                    .ToListAsync();

                // Calculate available time slots
                var availableSlots = GenerateAvailableTimeSlots(
                    serviceAvailability,
                    service.DurationInMinutes,
                    service.MaxConcurrentBookings,
                    existingBookings,
                    date);

                return Ok(ApiResponse<List<TimeSlotDto>>.SuccessResponse(availableSlots));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service availability");
                return StatusCode(500, ApiResponse<List<TimeSlotDto>>.ErrorResponse("An error occurred while getting availability"));
            }
        }

        #region Helper Methods

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
            var now = DateTime.Now;

            // Find active offers for this service
            var activeOffers = service.OfferItems?
                .Where(oi => oi.Offer.IsActive && oi.Offer.StartDate <= now && oi.Offer.EndDate >= now)
                .Select(oi => new ServiceOfferDto
                {
                    Id = oi.Offer.Id,
                    Name = oi.Offer.Name,
                    Description = oi.Offer.Description,
                    DiscountValue = oi.Offer.IsPercentage ? oi.Offer.DiscountPercentage : oi.Offer.DiscountAmount,
                    IsPercentage = oi.Offer.IsPercentage,
                    StartDate = oi.Offer.StartDate,
                    EndDate = oi.Offer.EndDate,
                    IsActive = oi.Offer.IsActive,
                    PromoCode = oi.Offer.PromoCode,
                    RequiresPromoCode = oi.Offer.RequiresPromoCode,
                    MaxUsage = oi.Offer.MaxUsage,
                    CurrentUsage = oi.Offer.CurrentUsage
                }).ToList();

            // Map availabilities if they're loaded
            var availabilities = service.Availabilities?
                .Select(a => new ServiceAvailabilityDto
                {
                    Id = a.Id,
                    DayOfWeek = a.DayOfWeek,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    IsAvailable = a.IsAvailable,
                    MaxBookingsPerSlot = a.MaxBookingsPerSlot,
                    SlotDurationMinutes = a.SlotDurationMinutes,
                    BufferTimeMinutes = a.BufferTimeMinutes
                }).ToList();

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

                // Extended properties
                ImageUrl = service.ImageUrl,
                CategoryId = service.CategoryId,
                CategoryName = service.Category?.Name,
                MaxConcurrentBookings = service.MaxConcurrentBookings,
                RequiresStaffAssignment = service.RequiresStaffAssignment,
                BufferTimeBeforeMinutes = service.BufferTimeBeforeMinutes,
                BufferTimeAfterMinutes = service.BufferTimeAfterMinutes,

                // Related data
                Availabilities = availabilities,
                ActiveOffers = activeOffers,

                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt
            };
        }

        private List<TimeSlotDto> GenerateAvailableTimeSlots(
            ServiceAvailability availability,
            int serviceDuration,
            int maxConcurrentBookings,
            List<Booking> existingBookings,
            DateTime date)
        {
            var slots = new List<TimeSlotDto>();
            var slotDuration = availability.SlotDurationMinutes;

            if (slotDuration <= 0)
            {
                // If slot duration is not set, use service duration
                slotDuration = serviceDuration;
            }

            var bufferTime = availability.BufferTimeMinutes;

            // Start time with date component from the requested date
            var startTime = new DateTime(
                date.Year, date.Month, date.Day,
                availability.StartTime.Hours, availability.StartTime.Minutes, 0);

            var endTime = new DateTime(
                date.Year, date.Month, date.Day,
                availability.EndTime.Hours, availability.EndTime.Minutes, 0);

            // Check if date is today and if some slots are already in the past
            if (date.Date == DateTime.Today && startTime < DateTime.Now)
            {
                // Adjust start time to the nearest future slot
                var minutesSinceMidnight = (int)DateTime.Now.TimeOfDay.TotalMinutes;
                var nextSlotMinutes = ((minutesSinceMidnight / slotDuration) + 1) * slotDuration;
                startTime = date.Date.AddMinutes(nextSlotMinutes);
            }

            // Generate slots until the end time
            var currentSlot = startTime;

            while (currentSlot.Add(TimeSpan.FromMinutes(serviceDuration)) <= endTime)
            {
                // Check how many bookings already exist in this time slot
                var bookingsInSlot = existingBookings.Count(b =>
                    (b.BookingTime <= currentSlot.TimeOfDay &&
                     b.BookingTime.Add(TimeSpan.FromMinutes(serviceDuration)) > currentSlot.TimeOfDay) ||
                    (b.BookingTime > currentSlot.TimeOfDay &&
                     b.BookingTime < currentSlot.TimeOfDay.Add(TimeSpan.FromMinutes(serviceDuration))));

                // If slot has capacity, add it to available slots
                if (bookingsInSlot < maxConcurrentBookings)
                {
                    slots.Add(new TimeSlotDto
                    {
                        StartTime = currentSlot,
                        EndTime = currentSlot.AddMinutes(serviceDuration),
                        AvailableSpots = maxConcurrentBookings - bookingsInSlot
                    });
                }

                // Move to next slot
                currentSlot = currentSlot.AddMinutes(slotDuration);
            }

            return slots;
        }

        #endregion
    }

    // Define additional DTOs if needed
    public class TimeSlotDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int AvailableSpots { get; set; }
    }
}

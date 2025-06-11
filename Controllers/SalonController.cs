using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs;
using stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs;
using stibe.api.Models.Entities;
using stibe.api.Services.Interfaces;
using System.Security.Claims;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationService _locationService;
        private readonly ILogger<SalonController> _logger;

        public SalonController(
            ApplicationDbContext context,
            ILocationService locationService,
            ILogger<SalonController> logger)
        {
            _context = context;
            _locationService = locationService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<SalonResponseDto>>> CreateSalon(CreateSalonRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid token"));
                }

                if (request.OpeningTime >= request.ClosingTime)
                {
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Opening time must be before closing time"));
                }

                // Get coordinates for the address
                var coordinates = await _locationService.GetCoordinatesAsync(
                    request.Address, request.City, request.State, request.ZipCode);

                var salon = new Salon
                {
                    Name = request.Name,
                    Description = request.Description,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    ZipCode = request.ZipCode,
                    Latitude = coordinates.Latitude,
                    Longitude = coordinates.Longitude,
                    PhoneNumber = request.PhoneNumber,
                    OpeningTime = request.OpeningTime,
                    ClosingTime = request.ClosingTime,
                    OwnerId = currentUserId.Value,
                    IsActive = true
                };

                _context.Salons.Add(salon);
                await _context.SaveChangesAsync();

                // Load the salon with owner information
                var createdSalon = await _context.Salons
                    .Include(s => s.Owner)
                    .Include(s => s.Services)
                    .FirstOrDefaultAsync(s => s.Id == salon.Id);

                if (createdSalon == null)
                {
                    return StatusCode(500, ApiResponse<SalonResponseDto>.ErrorResponse("Failed to create salon"));
                }

                var response = MapToSalonResponse(createdSalon);

                _logger.LogInformation($"Salon created successfully: {salon.Name} by user {currentUserId}");
                return Ok(ApiResponse<SalonResponseDto>.SuccessResponse(response, "Salon created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating salon");
                return StatusCode(500, ApiResponse<SalonResponseDto>.ErrorResponse("An error occurred while creating the salon"));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SalonResponseDto>>> GetSalon(int id)
        {
            try
            {
                var salon = await _context.Salons
                    .Include(s => s.Owner)
                    .Include(s => s.Services)
                    .Where(s => s.Id == id && !s.IsDeleted)
                    .FirstOrDefaultAsync();

                if (salon == null)
                {
                    return NotFound(ApiResponse<SalonResponseDto>.ErrorResponse("Salon not found"));
                }

                var response = MapToSalonResponse(salon);
                return Ok(ApiResponse<SalonResponseDto>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting salon");
                return StatusCode(500, ApiResponse<SalonResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("my-salons")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<List<SalonResponseDto>>>> GetMySalons()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<List<SalonResponseDto>>.ErrorResponse("Invalid token"));
                }

                var salons = await _context.Salons
                    .Include(s => s.Owner)
                    .Include(s => s.Services)
                    .Where(s => s.OwnerId == currentUserId.Value && !s.IsDeleted)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var response = new List<SalonResponseDto>();
                foreach (var salon in salons)
                {
                    response.Add(MapToSalonResponse(salon));
                }

                return Ok(ApiResponse<List<SalonResponseDto>>.SuccessResponse(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user salons");
                return StatusCode(500, ApiResponse<List<SalonResponseDto>>.ErrorResponse("An error occurred"));
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

        private SalonResponseDto MapToSalonResponse(Salon salon)
        {
            var serviceList = new List<ServiceResponseDto>();
            foreach (var service in salon.Services)
            {
                if (service.IsActive)
                {
                    serviceList.Add(new ServiceResponseDto
                    {
                        Id = service.Id,
                        Name = service.Name,
                        Description = service.Description,
                        Price = service.Price,
                        DurationInMinutes = service.DurationInMinutes,
                        IsActive = service.IsActive,
                        SalonId = service.SalonId,
                        SalonName = salon.Name,
                        CreatedAt = service.CreatedAt,
                        UpdatedAt = service.UpdatedAt
                    });
                }
            }

            return new SalonResponseDto
            {
                Id = salon.Id,
                Name = salon.Name,
                Description = salon.Description,
                Address = salon.Address,
                City = salon.City,
                State = salon.State,
                ZipCode = salon.ZipCode,
                Latitude = salon.Latitude,
                Longitude = salon.Longitude,
                PhoneNumber = salon.PhoneNumber,
                OpeningTime = salon.OpeningTime,
                ClosingTime = salon.ClosingTime,
                IsActive = salon.IsActive,
                OwnerId = salon.OwnerId,
                OwnerName = $"{salon.Owner.FirstName} {salon.Owner.LastName}",
                CreatedAt = salon.CreatedAt,
                UpdatedAt = salon.UpdatedAt,
                Services = serviceList
            };
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs;
using stibe.api.Models.Entities.PartnersEntity;
using System.Security.Claims;
using System.Text.Json;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SalonController> _logger;

        public SalonController(ApplicationDbContext context, ILogger<SalonController> logger)
        {
            _context = context;
            _logger = logger;
        }        [HttpPost]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<SalonResponseDto>>> CreateSalon([FromBody] CreateSalonRequestDto request)
        {
            try
            {
                // Log the incoming request data for debugging
                _logger.LogInformation($"🏢 CreateSalon called with request: Name={request?.Name}, City={request?.City}, State={request?.State}, Address={request?.Address}, ZipCode={request?.ZipCode}");
                
                // Check if the request is null (model binding failed)
                if (request == null)
                {
                    _logger.LogError("❌ CreateSalon request is null - model binding failed");
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid request data"));
                }
                
                // Log validation state
                if (!ModelState.IsValid)
                {
                    _logger.LogError("❌ Model validation failed: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Validation failed", errors));
                }
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid token"));
                }

                // Validate time format
                if (!TimeSpan.TryParse(request.OpeningTime, out var openingTime))
                {
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid opening time format. Expected format: HH:mm:ss"));
                }

                if (!TimeSpan.TryParse(request.ClosingTime, out var closingTime))
                {
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid closing time format. Expected format: HH:mm:ss"));
                }

                // Check if user already has a salon (if business rule applies)
                var existingSalons = await _context.Salons
                    .Where(s => s.OwnerId == currentUserId.Value && !s.IsDeleted)
                    .CountAsync();                // For now, allow multiple salons per owner
                // if (existingSalons > 0)
                // {
                //     return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("You already have a salon registered"));
                // }

                var salon = new Salon
                {
                    Name = request.Name,
                    Description = request.Description,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    ZipCode = request.ZipCode,
                    PhoneNumber = request.PhoneNumber,                    OpeningTime = openingTime,
                    ClosingTime = closingTime,
                    Latitude = request.CurrentLatitude,
                    Longitude = request.CurrentLongitude,
                    OwnerId = currentUserId.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Salons.Add(salon);
                await _context.SaveChangesAsync();

                var response = new SalonResponseDto
                {
                    Id = salon.Id,
                    Name = salon.Name,
                    Description = salon.Description,
                    Address = salon.Address,
                    City = salon.City,
                    State = salon.State,
                    ZipCode = salon.ZipCode,
                    PhoneNumber = salon.PhoneNumber,
                    OpeningTime = salon.OpeningTime,
                    ClosingTime = salon.ClosingTime,
                    Latitude = salon.Latitude,
                    Longitude = salon.Longitude,
                    IsActive = salon.IsActive,
                    OwnerId = salon.OwnerId,
                    CreatedAt = salon.CreatedAt,
                    UpdatedAt = salon.UpdatedAt
                };

                return Ok(ApiResponse<SalonResponseDto>.SuccessResponse(response, "Salon created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating salon");
                return StatusCode(500, ApiResponse<SalonResponseDto>.ErrorResponse("An error occurred while creating the salon"));
            }
        }

        [HttpPost("create-json")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<SalonResponseDto>>> CreateSalonJson([FromBody] CreateSalonJsonRequestDto request)
        {
            try
            {
                // Log the incoming request data for debugging
                _logger.LogInformation($"🏢 CreateSalonJson called with request: Name={request?.Name}, City={request?.City}, State={request?.State}");
                
                // Check if the request is null (model binding failed)
                if (request == null)
                {
                    _logger.LogError("❌ CreateSalonJson request is null - model binding failed");
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid request data"));
                }
                
                // Log validation state
                if (!ModelState.IsValid)
                {
                    _logger.LogError("❌ Model validation failed: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Validation failed", errors));
                }
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid token"));
                }

                // Validate time format
                if (!TimeSpan.TryParse(request.OpeningTime, out var openingTime))
                {
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid opening time format. Expected format: HH:mm:ss"));
                }

                if (!TimeSpan.TryParse(request.ClosingTime, out var closingTime))
                {
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid closing time format. Expected format: HH:mm:ss"));
                }

                // Serialize business hours to JSON string if provided
                string? businessHoursJson = null;
                if (request.BusinessHours != null && request.BusinessHours.Count > 0)
                {
                    try
                    {
                        businessHoursJson = JsonSerializer.Serialize(request.BusinessHours);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to serialize business hours");
                    }
                }

                var salon = new Salon
                {
                    Name = request.Name,
                    Description = request.Description,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    ZipCode = request.ZipCode,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    OpeningTime = openingTime,
                    ClosingTime = closingTime,
                    BusinessHours = businessHoursJson,
                    Latitude = request.CurrentLatitude,
                    Longitude = request.CurrentLongitude,
                    OwnerId = currentUserId.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Salons.Add(salon);
                await _context.SaveChangesAsync();

                var response = new SalonResponseDto
                {
                    Id = salon.Id,
                    Name = salon.Name,
                    Description = salon.Description,
                    Address = salon.Address,
                    City = salon.City,
                    State = salon.State,
                    ZipCode = salon.ZipCode,
                    PhoneNumber = salon.PhoneNumber,
                    Email = salon.Email,
                    OpeningTime = salon.OpeningTime,
                    ClosingTime = salon.ClosingTime,
                    BusinessHours = salon.BusinessHours,
                    Latitude = salon.Latitude,
                    Longitude = salon.Longitude,
                    IsActive = salon.IsActive,
                    OwnerId = salon.OwnerId,
                    CreatedAt = salon.CreatedAt,
                    UpdatedAt = salon.UpdatedAt
                };

                return Ok(ApiResponse<SalonResponseDto>.SuccessResponse(response, "Salon created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating salon via JSON");
                return StatusCode(500, ApiResponse<SalonResponseDto>.ErrorResponse("An error occurred while creating the salon"));
            }
        }

        [HttpGet]
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
                    .Where(s => s.OwnerId == currentUserId.Value && !s.IsDeleted)
                    .Select(s => new SalonResponseDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Address = s.Address,
                        City = s.City,
                        State = s.State,
                        ZipCode = s.ZipCode,
                        PhoneNumber = s.PhoneNumber,
                        Email = s.Email,
                        OpeningTime = s.OpeningTime,
                        ClosingTime = s.ClosingTime,
                        BusinessHours = s.BusinessHours,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        IsActive = s.IsActive,
                        OwnerId = s.OwnerId,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<SalonResponseDto>>.SuccessResponse(salons, "Salons retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving salons");
                return StatusCode(500, ApiResponse<List<SalonResponseDto>>.ErrorResponse("An error occurred while retrieving salons"));
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<ActionResult<ApiResponse<SalonResponseDto>>> GetSalon(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid token"));
                }

                var salon = await _context.Salons
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (salon == null)
                {
                    return NotFound(ApiResponse<SalonResponseDto>.ErrorResponse("Salon not found"));
                }

                // Check ownership (only owner or admin can view)
                if (userRole != "Admin" && salon.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only view your own salons");
                }

                var response = new SalonResponseDto
                {
                    Id = salon.Id,
                    Name = salon.Name,
                    Description = salon.Description,
                    Address = salon.Address,
                    City = salon.City,
                    State = salon.State,
                    ZipCode = salon.ZipCode,
                    PhoneNumber = salon.PhoneNumber,
                    Email = salon.Email,
                    OpeningTime = salon.OpeningTime,
                    ClosingTime = salon.ClosingTime,
                    BusinessHours = salon.BusinessHours,
                    Latitude = salon.Latitude,
                    Longitude = salon.Longitude,
                    IsActive = salon.IsActive,
                    OwnerId = salon.OwnerId,
                    CreatedAt = salon.CreatedAt,
                    UpdatedAt = salon.UpdatedAt
                };

                return Ok(ApiResponse<SalonResponseDto>.SuccessResponse(response, "Salon retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving salon");
                return StatusCode(500, ApiResponse<SalonResponseDto>.ErrorResponse("An error occurred while retrieving the salon"));
            }
        }

     
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        private string? GetCurrentUserRole()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }
    }
}

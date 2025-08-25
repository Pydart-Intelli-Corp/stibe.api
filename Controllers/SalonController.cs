using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SalonController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileService _fileService;

        public SalonController(ApplicationDbContext context, ILogger<SalonController> logger, IWebHostEnvironment environment, IFileService fileService)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _fileService = fileService;
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
                    PhoneNumber = request.PhoneNumber,
                    ServiceType = request.ServiceType,
                    GenderServices = request.GenderServices != null && request.GenderServices.Any() 
                        ? JsonSerializer.Serialize(request.GenderServices) 
                        : null,
                    Specializations = request.Specializations != null && request.Specializations.Any() 
                        ? JsonSerializer.Serialize(request.Specializations) 
                        : null,
                    BankAccountNumber = request.BankAccountNumber,
                    IFSCCode = request.IFSCCode,
                    BankName = request.BankName,
                    AccountHolderName = request.AccountHolderName,
                    GSTNumber = request.GSTNumber,
                    PANNumber = request.PANNumber,
                    OpeningTime = openingTime,
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
                    ServiceType = salon.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(salon.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(salon.GenderServices) 
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(salon.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(salon.Specializations) 
                        : new List<string>(),
                    BankAccountNumber = salon.BankAccountNumber,
                    IFSCCode = salon.IFSCCode,
                    BankName = salon.BankName,
                    AccountHolderName = salon.AccountHolderName,
                    GSTNumber = salon.GSTNumber,
                    PANNumber = salon.PANNumber,
                    OpeningTime = salon.OpeningTime,
                    ClosingTime = salon.ClosingTime,
                    Latitude = salon.Latitude,
                    Longitude = salon.Longitude,
                    IsActive = salon.IsActive,
                    OwnerId = salon.OwnerId,
                    CreatedAt = salon.CreatedAt,
                    UpdatedAt = salon.UpdatedAt,
                    ProfilePictureUrl = salon.ProfilePictureUrl ?? string.Empty,
                    ImageUrls = !string.IsNullOrEmpty(salon.ImageUrls) 
                        ? salon.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                        : new List<string>()
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
                    ServiceType = request.ServiceType,
                    GenderServices = request.GenderServices != null && request.GenderServices.Any() 
                        ? JsonSerializer.Serialize(request.GenderServices) 
                        : null,
                    Specializations = request.Specializations != null && request.Specializations.Any() 
                        ? JsonSerializer.Serialize(request.Specializations) 
                        : null,
                    OpeningTime = openingTime,
                    ClosingTime = closingTime,
                    BusinessHours = businessHoursJson,
                    Latitude = request.CurrentLatitude,
                    Longitude = request.CurrentLongitude,
                    OwnerId = currentUserId.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // Handle image URLs
                    ImageUrls = request.ImageUrls != null && request.ImageUrls.Any() 
                        ? JsonSerializer.Serialize(request.ImageUrls.Where(url => !string.IsNullOrEmpty(url)).ToList())
                        : null,
                    ProfilePictureUrl = !string.IsNullOrEmpty(request.ProfilePictureUrl) 
                        ? request.ProfilePictureUrl
                        : request.ImageUrls?.FirstOrDefault()
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
                    ServiceType = salon.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(salon.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(salon.GenderServices) 
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(salon.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(salon.Specializations) 
                        : new List<string>(),
                    OpeningTime = salon.OpeningTime,
                    ClosingTime = salon.ClosingTime,
                    BusinessHours = salon.BusinessHours,
                    Latitude = salon.Latitude,
                    Longitude = salon.Longitude,
                    IsActive = salon.IsActive,
                    OwnerId = salon.OwnerId,
                    CreatedAt = salon.CreatedAt,
                    UpdatedAt = salon.UpdatedAt,
                    ProfilePictureUrl = salon.ProfilePictureUrl ?? string.Empty,
                    ImageUrls = !string.IsNullOrEmpty(salon.ImageUrls) 
                        ? JsonSerializer.Deserialize<List<string>>(salon.ImageUrls) ?? new List<string>()
                        : new List<string>()
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
                    .ToListAsync();

                var salonDtos = salons.Select(s => new SalonResponseDto
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
                        ServiceType = s.ServiceType,
                        GenderServices = !string.IsNullOrEmpty(s.GenderServices) 
                            ? JsonSerializer.Deserialize<List<string>>(s.GenderServices) ?? new List<string>()
                            : new List<string>(),
                        Specializations = !string.IsNullOrEmpty(s.Specializations) 
                            ? JsonSerializer.Deserialize<List<string>>(s.Specializations) ?? new List<string>()
                            : new List<string>(),
                        OpeningTime = s.OpeningTime,
                        ClosingTime = s.ClosingTime,
                        BusinessHours = s.BusinessHours,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        IsActive = s.IsActive,
                        OwnerId = s.OwnerId,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        ProfilePictureUrl = s.ProfilePictureUrl ?? string.Empty,
                        ImageUrls = !string.IsNullOrEmpty(s.ImageUrls) 
                            ? JsonSerializer.Deserialize<List<string>>(s.ImageUrls) ?? new List<string>()
                            : new List<string>()
                    })
                    .ToList();

                return Ok(ApiResponse<List<SalonResponseDto>>.SuccessResponse(salonDtos, "Salons retrieved successfully"));
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
                    ServiceType = salon.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(salon.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(salon.GenderServices) ?? new List<string>()
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(salon.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(salon.Specializations) ?? new List<string>()
                        : new List<string>(),
                    OpeningTime = salon.OpeningTime,
                    ClosingTime = salon.ClosingTime,
                    BusinessHours = salon.BusinessHours,
                    Latitude = salon.Latitude,
                    Longitude = salon.Longitude,
                    IsActive = salon.IsActive,
                    OwnerId = salon.OwnerId,
                    CreatedAt = salon.CreatedAt,
                    UpdatedAt = salon.UpdatedAt,
                    ProfilePictureUrl = salon.ProfilePictureUrl ?? string.Empty,
                    ImageUrls = !string.IsNullOrEmpty(salon.ImageUrls) 
                        ? JsonSerializer.Deserialize<List<string>>(salon.ImageUrls) ?? new List<string>()
                        : new List<string>()
                };

                return Ok(ApiResponse<SalonResponseDto>.SuccessResponse(response, "Salon retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving salon");
                return StatusCode(500, ApiResponse<SalonResponseDto>.ErrorResponse("An error occurred while retrieving the salon"));
            }
        }

        [HttpPost("upload-image")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<object>>> UploadSalonImage(IFormFile image)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token"));
                }

                if (image == null || image.Length == 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("No image file provided"));
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid file type. Only JPG, PNG, and GIF files are allowed."));
                }

                // Validate file size (max 5MB)
                if (image.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("File size must be less than 5MB"));
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "salon-images");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Generate the URL
                var request = HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var imageUrl = $"{baseUrl}/uploads/salon-images/{fileName}";

                _logger.LogInformation($"✅ Salon image uploaded: {imageUrl}");

                return Ok(ApiResponse<object>.SuccessResponse(new { imageUrl }, "Image uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading salon image");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while uploading salon image"));
            }
        }

        [HttpDelete("{salonId}/images")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteSalonImages(int salonId, [FromBody] List<string> imageUrls)
        {
            try
            {
                _logger.LogInformation($"🗑️ DeleteSalonImages called for salon ID: {salonId} with {imageUrls?.Count ?? 0} images");
                
                if (imageUrls == null || !imageUrls.Any())
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("No image URLs provided"));
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
                }

                // Find the salon
                var salon = await _context.Salons.FirstOrDefaultAsync(s => s.Id == salonId && s.OwnerId == currentUserId);
                if (salon == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Salon not found or you don't have permission to modify it"));
                }

                // Get current image URLs from salon
                var currentImageUrls = string.IsNullOrEmpty(salon.ImageUrls) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(salon.ImageUrls) ?? new List<string>();

                // Remove the specified images from the salon's image list
                var updatedImageUrls = currentImageUrls.Where(url => !imageUrls.Contains(url)).ToList();
                
                // Update the salon record
                salon.ImageUrls = JsonSerializer.Serialize(updatedImageUrls);
                salon.ProfilePictureUrl = updatedImageUrls.FirstOrDefault(); // Update profile picture to first remaining image
                salon.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                // Delete the actual image files from storage
                var deletedImages = new List<string>();
                var failedDeletions = new List<string>();

                foreach (var imageUrl in imageUrls)
                {
                    try
                    {
                        await _fileService.DeleteFileAsync(imageUrl, "salon-images");
                        deletedImages.Add(imageUrl);
                        _logger.LogInformation($"✅ Successfully deleted image: {imageUrl}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Failed to delete image from storage: {imageUrl}");
                        failedDeletions.Add(imageUrl);
                    }
                }

                var response = new
                {
                    deletedImages = deletedImages,
                    failedDeletions = failedDeletions,
                    remainingImages = updatedImageUrls,
                    message = $"Successfully processed {deletedImages.Count} images, {failedDeletions.Count} failed"
                };

                _logger.LogInformation($"✅ Image deletion completed. Deleted: {deletedImages.Count}, Failed: {failedDeletions.Count}");
                return Ok(ApiResponse<object>.SuccessResponse(response, "Image deletion completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting salon images");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting images"));
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

        [HttpPut("{id}")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<SalonResponseDto>>> UpdateSalon(int id, [FromBody] UpdateSalonRequestDto request)
        {
            try
            {
                _logger.LogInformation($"🏢 UpdateSalon called for salon ID: {id}");
                
                if (request == null)
                {
                    _logger.LogError("❌ UpdateSalon request is null - model binding failed");
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Invalid request data"));
                }
                
                if (!ModelState.IsValid)
                {
                    _logger.LogError("❌ Model validation failed: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<SalonResponseDto>.ErrorResponse("Validation failed", errors));
                }
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SalonResponseDto>.ErrorResponse("User not authenticated"));
                }

                // Find the existing salon
                var existingSalon = await _context.Salons.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == currentUserId);
                if (existingSalon == null)
                {
                    return NotFound(ApiResponse<SalonResponseDto>.ErrorResponse("Salon not found or you don't have permission to update it"));
                }

                // Update salon properties
                if (!string.IsNullOrEmpty(request.Name))
                    existingSalon.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Description))
                    existingSalon.Description = request.Description;
                if (!string.IsNullOrEmpty(request.Address))
                    existingSalon.Address = request.Address;
                if (!string.IsNullOrEmpty(request.City))
                    existingSalon.City = request.City;
                if (!string.IsNullOrEmpty(request.State))
                    existingSalon.State = request.State;
                if (!string.IsNullOrEmpty(request.ZipCode))
                    existingSalon.ZipCode = request.ZipCode;
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    existingSalon.PhoneNumber = request.PhoneNumber;
                if (!string.IsNullOrEmpty(request.Email))
                    existingSalon.Email = request.Email;
                if (!string.IsNullOrEmpty(request.ServiceType))
                    existingSalon.ServiceType = request.ServiceType;
                if (request.GenderServices != null)
                    existingSalon.GenderServices = request.GenderServices.Any() 
                        ? JsonSerializer.Serialize(request.GenderServices) 
                        : null;
                if (request.Specializations != null)
                    existingSalon.Specializations = request.Specializations.Any() 
                        ? JsonSerializer.Serialize(request.Specializations) 
                        : null;
                if (!string.IsNullOrEmpty(request.OpeningTime))
                    existingSalon.OpeningTime = TimeSpan.Parse(request.OpeningTime);
                if (!string.IsNullOrEmpty(request.ClosingTime))
                    existingSalon.ClosingTime = TimeSpan.Parse(request.ClosingTime);
                if (request.IsActive.HasValue)
                    existingSalon.IsActive = request.IsActive.Value;
                    
                existingSalon.UpdatedAt = DateTime.UtcNow;
                
                // Update location if provided
                if (request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    existingSalon.Latitude = request.Latitude.Value;
                    existingSalon.Longitude = request.Longitude.Value;
                }
                
                // Update business hours if provided
                if (request.BusinessHours != null)
                {
                    existingSalon.BusinessHours = JsonSerializer.Serialize(request.BusinessHours);
                }
                
                // Update profile picture URL if provided
                if (!string.IsNullOrEmpty(request.ProfilePictureUrl))
                {
                    existingSalon.ProfilePictureUrl = request.ProfilePictureUrl;
                }
                else if (request.ProfilePictureUrl == "")
                {
                    // If ProfilePictureUrl is explicitly set to empty string, remove it
                    existingSalon.ProfilePictureUrl = null;
                }
                
                // Update image URLs if provided
                if (request.ImageUrls != null)
                {
                    existingSalon.ImageUrls = JsonSerializer.Serialize(request.ImageUrls);
                }

                await _context.SaveChangesAsync();

                var responseDto = new SalonResponseDto
                {
                    Id = existingSalon.Id,
                    Name = existingSalon.Name ?? "",
                    Description = existingSalon.Description ?? "",
                    Address = existingSalon.Address ?? "",
                    City = existingSalon.City ?? "",
                    State = existingSalon.State ?? "",
                    ZipCode = existingSalon.ZipCode ?? "",
                    PhoneNumber = existingSalon.PhoneNumber ?? "",
                    Email = existingSalon.Email ?? "",
                    ServiceType = existingSalon.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(existingSalon.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(existingSalon.GenderServices) ?? new List<string>()
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(existingSalon.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(existingSalon.Specializations) ?? new List<string>()
                        : new List<string>(),
                    OpeningTime = existingSalon.OpeningTime,
                    ClosingTime = existingSalon.ClosingTime,
                    BusinessHours = existingSalon.BusinessHours,
                    Latitude = existingSalon.Latitude,
                    Longitude = existingSalon.Longitude,
                    IsActive = existingSalon.IsActive,
                    OwnerId = existingSalon.OwnerId,
                    CreatedAt = existingSalon.CreatedAt,
                    UpdatedAt = existingSalon.UpdatedAt,
                    ProfilePictureUrl = existingSalon.ProfilePictureUrl ?? "",
                    ImageUrls = string.IsNullOrEmpty(existingSalon.ImageUrls) ? 
                        new List<string>() : 
                        JsonSerializer.Deserialize<List<string>>(existingSalon.ImageUrls) ?? new List<string>()
                };

                _logger.LogInformation($"✅ Salon updated successfully with ID: {existingSalon.Id}");
                return Ok(ApiResponse<SalonResponseDto>.SuccessResponse(responseDto, "Salon updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating salon");
                return StatusCode(500, ApiResponse<SalonResponseDto>.ErrorResponse("An error occurred while updating the salon"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SalonOwner")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteSalon(int id)
        {
            try
            {
                _logger.LogInformation($"🗑️ DeleteSalon called for salon ID: {id}");
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
                }

                // Find the existing salon
                var existingSalon = await _context.Salons.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == currentUserId);
                if (existingSalon == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Salon not found or you don't have permission to delete it"));
                }

                // Remove the salon from the database
                _context.Salons.Remove(existingSalon);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Salon deleted successfully with ID: {id}");
                return Ok(ApiResponse<object>.SuccessResponse(new { }, "Salon deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting salon");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting the salon"));
            }
        }

        [HttpGet("check-salon-phone-status/{phone}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> CheckSalonPhoneStatus(string phone)
        {
            try
            {
                if (string.IsNullOrEmpty(phone))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Phone number is required"));
                }

                // Clean the phone number to handle different formats
                var cleanPhone = Regex.Replace(phone, @"[^\d+]", "");

                var salon = await _context.Salons
                    .Where(s => s.PhoneNumber == cleanPhone && !s.IsDeleted)
                    .Select(s => new { s.PhoneNumber, s.Name, s.City, s.State })
                    .FirstOrDefaultAsync();

                if (salon == null)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { 
                        exists = false, 
                        status = "not_registered",
                        message = "Phone number is available for salon registration"
                    }, "Phone number not registered for any salon"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(new { 
                    exists = true, 
                    status = "registered",
                    salon = new {
                        phoneNumber = salon.PhoneNumber,
                        name = salon.Name,
                        location = $"{salon.City}, {salon.State}"
                    },
                    message = "Phone number is already registered for another salon"
                }, "Phone number is already registered for a salon"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking salon phone number status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while checking phone number status"));
            }
        }

        [HttpGet("check-salon-email-status/{email}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> CheckSalonEmailStatus(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Email address is required"));
                }

                // Validate email format
                if (!IsValidEmail(email))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid email format"));
                }

                var salon = await _context.Salons
                    .Where(s => s.Email.ToLower() == email.ToLower() && !s.IsDeleted)
                    .Select(s => new { s.Email, s.Name, s.City, s.State })
                    .FirstOrDefaultAsync();

                if (salon == null)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { 
                        exists = false, 
                        status = "not_registered",
                        message = "Email address is available for salon registration"
                    }, "Email address not registered for any salon"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(new { 
                    exists = true, 
                    status = "registered",
                    salon = new {
                        email = salon.Email,
                        name = salon.Name,
                        location = $"{salon.City}, {salon.State}"
                    },
                    message = "Email address is already registered for another salon"
                }, "Email address is already registered for a salon"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking salon email address status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while checking email address status"));
            }
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove all non-digit characters for validation
            var digitsOnly = new Regex(@"[^\d]").Replace(phoneNumber, "");

            // Most phone numbers are between 8 and 15 digits
            return digitsOnly.Length >= 8 && digitsOnly.Length <= 15;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use a simple regex pattern for email validation
                var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}

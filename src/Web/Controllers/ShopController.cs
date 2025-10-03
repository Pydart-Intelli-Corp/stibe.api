using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Models.Entities;
using stibe.api.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShopController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileService _fileService;
        private readonly IOtpService _otpService;


        public ShopController(ApplicationDbContext context, ILogger<ShopController> logger, IWebHostEnvironment environment, IFileService fileService, IOtpService otpService)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _fileService = fileService;
            _otpService = otpService;
        }        [HttpPost]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ShopResponseDto>>> CreateShop([FromBody] CreateShopRequestDto request)
        {
            try
            {
                // Log the incoming request data for debugging
                _logger.LogInformation($"🏢 CreateShop called with request: Name={request?.Name}, City={request?.City}, State={request?.State}, Address={request?.Address}, ZipCode={request?.ZipCode}");
                
                // Check if the request is null (model binding failed)
                if (request == null)
                {
                    _logger.LogError("❌ CreateShop request is null - model binding failed");
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid request data"));
                }
                
                // Log validation state
                if (!ModelState.IsValid)
                {
                    _logger.LogError("❌ Model validation failed: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Validation failed", errors));
                }
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid token"));
                }

                // Validate time format
                if (!TimeSpan.TryParse(request.OpeningTime, out var openingTime))
                {
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid opening time format. Expected format: HH:mm:ss"));
                }

                if (!TimeSpan.TryParse(request.ClosingTime, out var closingTime))
                {
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid closing time format. Expected format: HH:mm:ss"));
                }

                // Check if user already has a shop (if business rule applies)
                var existingShops = await _context.Shops
                    .Where(s => s.OwnerId == currentUserId.Value && !s.IsDeleted)
                    .CountAsync();
                
                // Automatically set as default if this is the first shop
                bool isDefault = existingShops == 0;

                // For now, allow multiple shops per owner
                // if (existingShops > 0)
                // {
                //     return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("You already have a shop registered"));
                // }

                var shop = new Shop
                {
                    Name = request.Name,
                    Description = request.Description,
                    Address = request.Address,
                    City = request.City,
                    District = request.District,
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
                    IsDefault = isDefault,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Shops.Add(shop);
                await _context.SaveChangesAsync();

                var response = new ShopResponseDto
                {
                    Id = shop.Id,
                    Name = shop.Name,
                    Description = shop.Description,
                    Address = shop.Address,
                    City = shop.City,
                    State = shop.State,
                    ZipCode = shop.ZipCode,
                    District = shop.District,
                    PhoneNumber = shop.PhoneNumber,
                    Email = shop.Email,
                    ServiceType = shop.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(shop.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.GenderServices) 
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(shop.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.Specializations) 
                        : new List<string>(),
                    // Bank Details
                    BankAccountNumber = shop.BankAccountNumber,
                    IFSCCode = shop.IFSCCode,
                    BankName = shop.BankName,
                    AccountHolderName = shop.AccountHolderName,
                    // Tax Details
                    GSTNumber = shop.GSTNumber,
                    PANNumber = shop.PANNumber,
                    OpeningTime = shop.OpeningTime,
                    ClosingTime = shop.ClosingTime,
                    BusinessHours = shop.BusinessHours,
                    Latitude = shop.Latitude,
                    Longitude = shop.Longitude,
                    IsActive = shop.IsActive,
                    IsDefault = shop.IsDefault,
                    OwnerId = shop.OwnerId,
                    CreatedAt = shop.CreatedAt,
                    UpdatedAt = shop.UpdatedAt,
                    ProfilePictureUrl = shop.ProfilePictureUrl ?? string.Empty,
                    ImageUrls = !string.IsNullOrEmpty(shop.ImageUrls) 
                        ? shop.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                        : new List<string>()
                };

                return Ok(ApiResponse<ShopResponseDto>.SuccessResponse(response, "Shop created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shop");
                return StatusCode(500, ApiResponse<ShopResponseDto>.ErrorResponse("An error occurred while creating the shop"));
            }
        }

        [HttpPost("create-json")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ShopResponseDto>>> CreateShopJson([FromBody] CreateShopJsonRequestDto request)
        {
            try
            {
                // Log the incoming request data for debugging
                _logger.LogInformation($"🏢 CreateShopJson called with request: Name={request?.Name}, City={request?.City}, State={request?.State}");
                
                // Check if the request is null (model binding failed)
                if (request == null)
                {
                    _logger.LogError("❌ CreateShopJson request is null - model binding failed");
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid request data"));
                }
                
                // Log validation state
                if (!ModelState.IsValid)
                {
                    _logger.LogError("❌ Model validation failed: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Validation failed", errors));
                }
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid token"));
                }

                // Validate time format
                if (!TimeSpan.TryParse(request.OpeningTime, out var openingTime))
                {
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid opening time format. Expected format: HH:mm:ss"));
                }

                if (!TimeSpan.TryParse(request.ClosingTime, out var closingTime))
                {
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid closing time format. Expected format: HH:mm:ss"));
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

                var shop = new Shop
                {
                    Name = request.Name,
                    Description = request.Description,
                    Address = request.Address,
                    City = request.City,
                    District = request.District,
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
                    // Bank Details
                    BankAccountNumber = request.BankAccountNumber,
                    IFSCCode = request.IFSCCode,
                    BankName = request.BankName,
                    AccountHolderName = request.AccountHolderName,
                    // Tax Details
                    GSTNumber = request.GSTNumber,
                    GSTStateCode = request.GSTStateCode,
                    GSTStateName = request.GSTStateName,
                    GSTPANNumber = request.GSTPANNumber,
                    GSTEntityNumber = request.GSTEntityNumber,
                    GSTEntityType = request.GSTEntityType,
                    GSTValidatedAt = !string.IsNullOrEmpty(request.GSTNumber) ? DateTime.UtcNow : null,
                    PANNumber = request.PANNumber,
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

                _context.Shops.Add(shop);
                await _context.SaveChangesAsync();

                var response = new ShopResponseDto
                {
                    Id = shop.Id,
                    Name = shop.Name,
                    Description = shop.Description,
                    Address = shop.Address,
                    City = shop.City,
                    State = shop.State,
                    ZipCode = shop.ZipCode,
                    District = shop.District,
                    PhoneNumber = shop.PhoneNumber,
                    Email = shop.Email,
                    ServiceType = shop.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(shop.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.GenderServices) 
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(shop.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.Specializations) 
                        : new List<string>(),
                    // Bank Details
                    BankAccountNumber = shop.BankAccountNumber,
                    IFSCCode = shop.IFSCCode,
                    BankName = shop.BankName,
                    AccountHolderName = shop.AccountHolderName,
                    // Tax Details
                    GSTNumber = shop.GSTNumber,
                    PANNumber = shop.PANNumber,
                    OpeningTime = shop.OpeningTime,
                    ClosingTime = shop.ClosingTime,
                    BusinessHours = shop.BusinessHours,
                    Latitude = shop.Latitude,
                    Longitude = shop.Longitude,
                    IsActive = shop.IsActive,
                    OwnerId = shop.OwnerId,
                    CreatedAt = shop.CreatedAt,
                    UpdatedAt = shop.UpdatedAt,
                    ProfilePictureUrl = shop.ProfilePictureUrl ?? string.Empty,
                    ImageUrls = !string.IsNullOrEmpty(shop.ImageUrls) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>()
                        : new List<string>()
                };

                return Ok(ApiResponse<ShopResponseDto>.SuccessResponse(response, "Shop created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shop via JSON");
                return StatusCode(500, ApiResponse<ShopResponseDto>.ErrorResponse("An error occurred while creating the shop"));
            }
        }

        [HttpGet]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<List<ShopResponseDto>>>> GetMyShops()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<List<ShopResponseDto>>.ErrorResponse("Invalid token"));
                }

                // Auto-set default shop if only one exists
                await AutoSetDefaultShopIfNeeded(currentUserId.Value);

                var shops = await _context.Shops
                    .Where(s => s.OwnerId == currentUserId.Value && !s.IsDeleted)
                    .ToListAsync();

                var shopDtos = shops.Select(s => new ShopResponseDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Address = s.Address,
                        City = s.City,
                        State = s.State,
                        ZipCode = s.ZipCode,
                        District = s.District,
                        PhoneNumber = s.PhoneNumber,
                        Email = s.Email,
                        ServiceType = s.ServiceType,
                        GenderServices = !string.IsNullOrEmpty(s.GenderServices) 
                            ? JsonSerializer.Deserialize<List<string>>(s.GenderServices) ?? new List<string>()
                            : new List<string>(),
                        Specializations = !string.IsNullOrEmpty(s.Specializations) 
                            ? JsonSerializer.Deserialize<List<string>>(s.Specializations) ?? new List<string>()
                            : new List<string>(),
                        // Bank Details
                        BankAccountNumber = s.BankAccountNumber,
                        IFSCCode = s.IFSCCode,
                        BankName = s.BankName,
                        AccountHolderName = s.AccountHolderName,
                        // Tax Details
                        GSTNumber = s.GSTNumber,
                        PANNumber = s.PANNumber,
                        OpeningTime = s.OpeningTime,
                        ClosingTime = s.ClosingTime,
                        BusinessHours = s.BusinessHours,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        IsActive = s.IsActive,
                        IsDefault = s.IsDefault,
                        OwnerId = s.OwnerId,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        ProfilePictureUrl = s.ProfilePictureUrl ?? string.Empty,
                        ImageUrls = !string.IsNullOrEmpty(s.ImageUrls) 
                            ? JsonSerializer.Deserialize<List<string>>(s.ImageUrls) ?? new List<string>()
                            : new List<string>()
                    })
                    .ToList();

                return Ok(ApiResponse<List<ShopResponseDto>>.SuccessResponse(shopDtos, "Shops retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shops");
                return StatusCode(500, ApiResponse<List<ShopResponseDto>>.ErrorResponse("An error occurred while retrieving shops"));
            }
        }

        [HttpGet("saved-bank-details")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<SavedBankDetailsDto>>> GetSavedBankDetails()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<SavedBankDetailsDto>.ErrorResponse("Invalid token"));
                }

                // Find the most recent shop with complete bank details
                var shopWithBankDetails = await _context.Shops
                    .Where(s => s.OwnerId == currentUserId.Value && 
                               !s.IsDeleted &&
                               !string.IsNullOrEmpty(s.BankAccountNumber) &&
                               !string.IsNullOrEmpty(s.IFSCCode) &&
                               !string.IsNullOrEmpty(s.AccountHolderName))
                    .OrderByDescending(s => s.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (shopWithBankDetails == null)
                {
                    return NotFound(ApiResponse<SavedBankDetailsDto>.ErrorResponse("No saved bank details found"));
                }

                var savedBankDetails = new SavedBankDetailsDto
                {
                    BankAccountNumber = shopWithBankDetails.BankAccountNumber,
                    IFSCCode = shopWithBankDetails.IFSCCode,
                    BankName = shopWithBankDetails.BankName,
                    AccountHolderName = shopWithBankDetails.AccountHolderName,
                    GSTNumber = shopWithBankDetails.GSTNumber,
                    PANNumber = shopWithBankDetails.PANNumber,
                    ShopName = shopWithBankDetails.Name,
                    ShopId = shopWithBankDetails.Id
                };

                return Ok(ApiResponse<SavedBankDetailsDto>.SuccessResponse(savedBankDetails, "Saved bank details retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving saved bank details");
                return StatusCode(500, ApiResponse<SavedBankDetailsDto>.ErrorResponse("An error occurred while retrieving saved bank details"));
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "ShopOwner,Admin")]
        public async Task<ActionResult<ApiResponse<ShopResponseDto>>> GetShop(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid token"));
                }

                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ShopResponseDto>.ErrorResponse("Shop not found"));
                }

                // Check ownership (only owner or admin can view)
                if (userRole != "Admin" && shop.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only view your own shops");
                }

                var response = new ShopResponseDto
                {
                    Id = shop.Id,
                    Name = shop.Name,
                    Description = shop.Description,
                    Address = shop.Address,
                    City = shop.City,
                    State = shop.State,
                    ZipCode = shop.ZipCode,
                    District = shop.District,
                    PhoneNumber = shop.PhoneNumber,
                    Email = shop.Email,
                    ServiceType = shop.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(shop.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.GenderServices) ?? new List<string>()
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(shop.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.Specializations) ?? new List<string>()
                        : new List<string>(),
                    // Bank Details
                    BankAccountNumber = shop.BankAccountNumber,
                    IFSCCode = shop.IFSCCode,
                    BankName = shop.BankName,
                    AccountHolderName = shop.AccountHolderName,
                    // Tax Details
                    GSTNumber = shop.GSTNumber,
                    PANNumber = shop.PANNumber,
                    OpeningTime = shop.OpeningTime,
                    ClosingTime = shop.ClosingTime,
                    BusinessHours = shop.BusinessHours,
                    Latitude = shop.Latitude,
                    Longitude = shop.Longitude,
                    IsActive = shop.IsActive,
                    IsDefault = shop.IsDefault,
                    OwnerId = shop.OwnerId,
                    CreatedAt = shop.CreatedAt,
                    UpdatedAt = shop.UpdatedAt,
                    ProfilePictureUrl = shop.ProfilePictureUrl ?? string.Empty,
                    ImageUrls = !string.IsNullOrEmpty(shop.ImageUrls) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>()
                        : new List<string>()
                };

                return Ok(ApiResponse<ShopResponseDto>.SuccessResponse(response, "Shop retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shop");
                return StatusCode(500, ApiResponse<ShopResponseDto>.ErrorResponse("An error occurred while retrieving the shop"));
            }
        }

        [HttpPost("upload-image")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<object>>> UploadShopImage(IFormFile image)
        {
            try
            {
                _logger.LogInformation("=== SHOP IMAGE UPLOAD STARTED (AZURE STORAGE) ===");
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token"));
                }

                if (image == null || image.Length == 0)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("No image file provided"));
                }

                _logger.LogInformation("Shop image upload for user ID: {UserId}", currentUserId);
                _logger.LogInformation("Image file details - Name: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}", 
                    image.FileName, image.Length, image.ContentType);

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

                _logger.LogInformation("File extension: {Extension}", fileExtension);

                // Use the IFileService to upload to Azure Blob Storage
                var imageUrl = await _fileService.UploadFileAsync(image, "shop-images");
                
                _logger.LogInformation("Shop image uploaded successfully to Azure. URL: {ImageUrl}", imageUrl);
                _logger.LogInformation("=== SHOP IMAGE UPLOAD COMPLETED SUCCESSFULLY (AZURE STORAGE) ===");

                return Ok(ApiResponse<object>.SuccessResponse(new { imageUrl }, "Image uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading shop image");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while uploading shop image"));
            }
        }

        [HttpGet("{id}/bank-details")]
        [Authorize(Roles = "ShopOwner,Admin")]
        public async Task<ActionResult<ApiResponse<ShopBankDetailsDto>>> GetShopBankDetails(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopBankDetailsDto>.ErrorResponse("Invalid token"));
                }

                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<ShopBankDetailsDto>.ErrorResponse("Shop not found"));
                }

                // Check ownership (only owner or admin can view bank details)
                if (userRole != "Admin" && shop.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only view your own shop's bank details");
                }

                var bankDetails = new ShopBankDetailsDto
                {
                    ShopId = shop.Id,
                    ShopName = shop.Name,
                    BankAccountNumber = shop.BankAccountNumber,
                    IFSCCode = shop.IFSCCode,
                    BankName = shop.BankName,
                    AccountHolderName = shop.AccountHolderName,
                    GSTNumber = shop.GSTNumber,
                    PANNumber = shop.PANNumber,
                    IsActive = shop.IsActive,
                    UpdatedAt = shop.UpdatedAt
                };

                return Ok(ApiResponse<ShopBankDetailsDto>.SuccessResponse(bankDetails, "Bank details retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shop bank details");
                return StatusCode(500, ApiResponse<ShopBankDetailsDto>.ErrorResponse("An error occurred while retrieving the shop bank details"));
            }
        }

        [HttpPut("{id}/bank-details")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ShopBankDetailsDto>>> UpdateShopBankDetails(int id, [FromBody] UpdateShopBankDetailsDto request)
        {
            try
            {
                _logger.LogInformation($"🏦 UpdateShopBankDetails called for shop ID: {id}");
                
                if (request == null)
                {
                    return BadRequest(ApiResponse<ShopBankDetailsDto>.ErrorResponse("Invalid request data"));
                }
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<ShopBankDetailsDto>.ErrorResponse("Validation failed", errors));
                }
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopBankDetailsDto>.ErrorResponse("User not authenticated"));
                }

                // Find the existing shop
                var existingShop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == currentUserId);
                if (existingShop == null)
                {
                    return NotFound(ApiResponse<ShopBankDetailsDto>.ErrorResponse("Shop not found or you don't have permission to update it"));
                }

                // Update bank details
                if (!string.IsNullOrEmpty(request.BankAccountNumber))
                    existingShop.BankAccountNumber = request.BankAccountNumber;
                if (!string.IsNullOrEmpty(request.IFSCCode))
                    existingShop.IFSCCode = request.IFSCCode;
                if (!string.IsNullOrEmpty(request.BankName))
                    existingShop.BankName = request.BankName;
                if (!string.IsNullOrEmpty(request.AccountHolderName))
                    existingShop.AccountHolderName = request.AccountHolderName;
                if (!string.IsNullOrEmpty(request.GSTNumber))
                    existingShop.GSTNumber = request.GSTNumber;
                if (!string.IsNullOrEmpty(request.PANNumber))
                    existingShop.PANNumber = request.PANNumber;
                    
                existingShop.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var responseDto = new ShopBankDetailsDto
                {
                    ShopId = existingShop.Id,
                    ShopName = existingShop.Name,
                    BankAccountNumber = existingShop.BankAccountNumber,
                    IFSCCode = existingShop.IFSCCode,
                    BankName = existingShop.BankName,
                    AccountHolderName = existingShop.AccountHolderName,
                    GSTNumber = existingShop.GSTNumber,
                    PANNumber = existingShop.PANNumber,
                    IsActive = existingShop.IsActive,
                    UpdatedAt = existingShop.UpdatedAt
                };

                _logger.LogInformation($"✅ Shop bank details updated successfully for shop ID: {existingShop.Id}");
                return Ok(ApiResponse<ShopBankDetailsDto>.SuccessResponse(responseDto, "Bank details updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shop bank details");
                return StatusCode(500, ApiResponse<ShopBankDetailsDto>.ErrorResponse("An error occurred while updating the shop bank details"));
            }
        }

        [HttpDelete("{shopId}/images")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteShopImages(int shopId, [FromBody] List<string> imageUrls)
        {
            try
            {
                _logger.LogInformation($"🗑️ DeleteShopImages called for shop ID: {shopId} with {imageUrls?.Count ?? 0} images");
                
                if (imageUrls == null || !imageUrls.Any())
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("No image URLs provided"));
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
                }

                // Find the shop
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId);
                if (shop == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Shop not found or you don't have permission to modify it"));
                }

                // Get current image URLs from shop
                var currentImageUrls = string.IsNullOrEmpty(shop.ImageUrls) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>();

                // Remove the specified images from the shop's image list
                var updatedImageUrls = currentImageUrls.Where(url => !imageUrls.Contains(url)).ToList();
                
                // Update the shop record
                shop.ImageUrls = JsonSerializer.Serialize(updatedImageUrls);
                shop.ProfilePictureUrl = updatedImageUrls.FirstOrDefault(); // Update profile picture to first remaining image
                shop.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                // Delete the actual image files from storage
                var deletedImages = new List<string>();
                var failedDeletions = new List<string>();

                foreach (var imageUrl in imageUrls)
                {
                    try
                    {
                        await _fileService.DeleteFileAsync(imageUrl, "shop-images");
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
                _logger.LogError(ex, "Error deleting shop images");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting images"));
            }
        }

        [HttpPut("{id}/set-default")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ShopResponseDto>>> SetDefaultShop(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("User not authenticated"));
                }

                // Find the shop to set as default
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == currentUserId && !s.IsDeleted);
                if (shop == null)
                {
                    return NotFound(ApiResponse<ShopResponseDto>.ErrorResponse("Shop not found or you don't have permission to modify it"));
                }

                // Remove default from all other shops of this owner
                var otherShops = await _context.Shops
                    .Where(s => s.OwnerId == currentUserId.Value && s.Id != id && !s.IsDeleted)
                    .ToListAsync();
                
                foreach (var otherShop in otherShops)
                {
                    otherShop.IsDefault = false;
                }

                // Set this shop as default
                shop.IsDefault = true;
                shop.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new ShopResponseDto
                {
                    Id = shop.Id,
                    Name = shop.Name ?? "",
                    Description = shop.Description ?? "",
                    Address = shop.Address ?? "",
                    City = shop.City ?? "",
                    State = shop.State ?? "",
                    ZipCode = shop.ZipCode ?? "",
                    District = shop.District ?? "",
                    PhoneNumber = shop.PhoneNumber ?? "",
                    Email = shop.Email ?? "",
                    ServiceType = shop.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(shop.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.GenderServices) ?? new List<string>()
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(shop.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.Specializations) ?? new List<string>()
                        : new List<string>(),
                    BankAccountNumber = shop.BankAccountNumber,
                    IFSCCode = shop.IFSCCode,
                    BankName = shop.BankName,
                    AccountHolderName = shop.AccountHolderName,
                    GSTNumber = shop.GSTNumber,
                    PANNumber = shop.PANNumber,
                    OpeningTime = shop.OpeningTime,
                    ClosingTime = shop.ClosingTime,
                    BusinessHours = shop.BusinessHours,
                    Latitude = shop.Latitude,
                    Longitude = shop.Longitude,
                    IsActive = shop.IsActive,
                    IsDefault = shop.IsDefault,
                    OwnerId = shop.OwnerId,
                    CreatedAt = shop.CreatedAt,
                    UpdatedAt = shop.UpdatedAt,
                    ProfilePictureUrl = shop.ProfilePictureUrl ?? "",
                    ImageUrls = string.IsNullOrEmpty(shop.ImageUrls) ? 
                        new List<string>() : 
                        JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>()
                };

                return Ok(ApiResponse<ShopResponseDto>.SuccessResponse(response, "Default shop updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default shop");
                return StatusCode(500, ApiResponse<ShopResponseDto>.ErrorResponse("An error occurred while setting the default shop"));
            }
        }

        [HttpPost("change-status-with-otp")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ShopResponseDto>>> ChangeShopStatusWithOtp([FromBody] ShopStatusChangeRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("User not authenticated"));
                }

                var currentUserEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("User email not found in token"));
                }

                // Verify email matches current user's email (owner email)
                if (!string.Equals(currentUserEmail, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Email does not match the shop owner's registered email"));
                }

                // Verify OTP first
                var otpResult = await _otpService.VerifyOtpAsync(request.Email, request.OtpCode, OtpEntity.PURPOSE_SHOP_STATUS_CHANGE);
                if (!otpResult.Success)
                {
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse($"OTP verification failed: {otpResult.Message}"));
                }

                // Find the shop
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == request.ShopId && s.OwnerId == currentUserId && !s.IsDeleted);
                if (shop == null)
                {
                    return NotFound(ApiResponse<ShopResponseDto>.ErrorResponse("Shop not found or you don't have permission to modify it"));
                }

                // Update shop status
                shop.IsActive = request.IsActive;
                shop.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new ShopResponseDto
                {
                    Id = shop.Id,
                    Name = shop.Name ?? "",
                    Description = shop.Description ?? "",
                    Address = shop.Address ?? "",
                    City = shop.City ?? "",
                    State = shop.State ?? "",
                    ZipCode = shop.ZipCode ?? "",
                    District = shop.District ?? "",
                    PhoneNumber = shop.PhoneNumber ?? "",
                    Email = shop.Email ?? "",
                    ServiceType = shop.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(shop.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.GenderServices) ?? new List<string>()
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(shop.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.Specializations) ?? new List<string>()
                        : new List<string>(),
                    BankAccountNumber = shop.BankAccountNumber,
                    IFSCCode = shop.IFSCCode,
                    BankName = shop.BankName,
                    AccountHolderName = shop.AccountHolderName,
                    GSTNumber = shop.GSTNumber,
                    PANNumber = shop.PANNumber,
                    OpeningTime = shop.OpeningTime,
                    ClosingTime = shop.ClosingTime,
                    BusinessHours = shop.BusinessHours,
                    Latitude = shop.Latitude,
                    Longitude = shop.Longitude,
                    IsActive = shop.IsActive,
                    IsDefault = shop.IsDefault,
                    OwnerId = shop.OwnerId,
                    CreatedAt = shop.CreatedAt,
                    UpdatedAt = shop.UpdatedAt,
                    ProfilePictureUrl = shop.ProfilePictureUrl ?? "",
                    ImageUrls = string.IsNullOrEmpty(shop.ImageUrls) ? 
                        new List<string>() : 
                        JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>()
                };

                return Ok(ApiResponse<ShopResponseDto>.SuccessResponse(response, $"Shop {(request.IsActive ? "activated" : "deactivated")} successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing shop status with OTP");
                return StatusCode(500, ApiResponse<ShopResponseDto>.ErrorResponse("An error occurred while changing shop status"));
            }
        }

        [HttpPost("set-default-with-otp")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ShopResponseDto>>> SetDefaultShopWithOtp([FromBody] ShopDefaultChangeRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("User not authenticated"));
                }

                var currentUserEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("User email not found in token"));
                }

                // Verify email matches current user's email (owner email)
                if (!string.Equals(currentUserEmail, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Email does not match the shop owner's registered email"));
                }

                // Verify OTP first
                var otpResult = await _otpService.VerifyOtpAsync(request.Email, request.OtpCode, OtpEntity.PURPOSE_SHOP_DEFAULT_CHANGE);
                if (!otpResult.Success)
                {
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse($"OTP verification failed: {otpResult.Message}"));
                }

                // Find the shop
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == request.ShopId && s.OwnerId == currentUserId && !s.IsDeleted);
                if (shop == null)
                {
                    return NotFound(ApiResponse<ShopResponseDto>.ErrorResponse("Shop not found or you don't have permission to modify it"));
                }

                // Remove default from all other shops of this owner
                var otherShops = await _context.Shops
                    .Where(s => s.OwnerId == currentUserId.Value && s.Id != request.ShopId && !s.IsDeleted)
                    .ToListAsync();
                
                foreach (var otherShop in otherShops)
                {
                    otherShop.IsDefault = false;
                }

                // Set this shop as default
                shop.IsDefault = true;
                shop.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new ShopResponseDto
                {
                    Id = shop.Id,
                    Name = shop.Name ?? "",
                    Description = shop.Description ?? "",
                    Address = shop.Address ?? "",
                    City = shop.City ?? "",
                    State = shop.State ?? "",
                    ZipCode = shop.ZipCode ?? "",
                    District = shop.District ?? "",
                    PhoneNumber = shop.PhoneNumber ?? "",
                    Email = shop.Email ?? "",
                    ServiceType = shop.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(shop.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.GenderServices) ?? new List<string>()
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(shop.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(shop.Specializations) ?? new List<string>()
                        : new List<string>(),
                    BankAccountNumber = shop.BankAccountNumber,
                    IFSCCode = shop.IFSCCode,
                    BankName = shop.BankName,
                    AccountHolderName = shop.AccountHolderName,
                    GSTNumber = shop.GSTNumber,
                    PANNumber = shop.PANNumber,
                    OpeningTime = shop.OpeningTime,
                    ClosingTime = shop.ClosingTime,
                    BusinessHours = shop.BusinessHours,
                    Latitude = shop.Latitude,
                    Longitude = shop.Longitude,
                    IsActive = shop.IsActive,
                    IsDefault = shop.IsDefault,
                    OwnerId = shop.OwnerId,
                    CreatedAt = shop.CreatedAt,
                    UpdatedAt = shop.UpdatedAt,
                    ProfilePictureUrl = shop.ProfilePictureUrl ?? "",
                    ImageUrls = string.IsNullOrEmpty(shop.ImageUrls) ? 
                        new List<string>() : 
                        JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>()
                };

                return Ok(ApiResponse<ShopResponseDto>.SuccessResponse(response, "Default shop updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default shop with OTP");
                return StatusCode(500, ApiResponse<ShopResponseDto>.ErrorResponse("An error occurred while setting the default shop"));
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

        private string? GetCurrentUserEmail()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<ShopResponseDto>>> UpdateShop(int id, [FromBody] UpdateShopRequestDto request)
        {
            try
            {
                _logger.LogInformation($"🏢 UpdateShop called for shop ID: {id}");
                
                if (request == null)
                {
                    _logger.LogError("❌ UpdateShop request is null - model binding failed");
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Invalid request data"));
                }
                
                if (!ModelState.IsValid)
                {
                    _logger.LogError("❌ Model validation failed: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<ShopResponseDto>.ErrorResponse("Validation failed", errors));
                }
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<ShopResponseDto>.ErrorResponse("User not authenticated"));
                }

                // Find the existing shop
                var existingShop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == currentUserId);
                if (existingShop == null)
                {
                    return NotFound(ApiResponse<ShopResponseDto>.ErrorResponse("Shop not found or you don't have permission to update it"));
                }

                // Update shop properties
                if (!string.IsNullOrEmpty(request.Name))
                    existingShop.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Description))
                    existingShop.Description = request.Description;
                if (!string.IsNullOrEmpty(request.Address))
                    existingShop.Address = request.Address;
                if (!string.IsNullOrEmpty(request.City))
                    existingShop.City = request.City;
                if (!string.IsNullOrEmpty(request.State))
                    existingShop.State = request.State;
                if (!string.IsNullOrEmpty(request.ZipCode))
                    existingShop.ZipCode = request.ZipCode;
                if (!string.IsNullOrEmpty(request.District))
                    existingShop.District = request.District;
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    existingShop.PhoneNumber = request.PhoneNumber;
                if (!string.IsNullOrEmpty(request.Email))
                    existingShop.Email = request.Email;
                if (!string.IsNullOrEmpty(request.ServiceType))
                    existingShop.ServiceType = request.ServiceType;
                if (request.GenderServices != null)
                    existingShop.GenderServices = request.GenderServices.Any() 
                        ? JsonSerializer.Serialize(request.GenderServices) 
                        : null;
                if (request.Specializations != null)
                    existingShop.Specializations = request.Specializations.Any() 
                        ? JsonSerializer.Serialize(request.Specializations) 
                        : null;
                // Update bank details
                if (!string.IsNullOrEmpty(request.BankAccountNumber))
                    existingShop.BankAccountNumber = request.BankAccountNumber;
                if (!string.IsNullOrEmpty(request.IFSCCode))
                    existingShop.IFSCCode = request.IFSCCode;
                if (!string.IsNullOrEmpty(request.BankName))
                    existingShop.BankName = request.BankName;
                if (!string.IsNullOrEmpty(request.AccountHolderName))
                    existingShop.AccountHolderName = request.AccountHolderName;
                // Update tax details
                if (!string.IsNullOrEmpty(request.GSTNumber))
                    existingShop.GSTNumber = request.GSTNumber;
                if (!string.IsNullOrEmpty(request.PANNumber))
                    existingShop.PANNumber = request.PANNumber;
                if (!string.IsNullOrEmpty(request.OpeningTime))
                    existingShop.OpeningTime = TimeSpan.Parse(request.OpeningTime);
                if (!string.IsNullOrEmpty(request.ClosingTime))
                    existingShop.ClosingTime = TimeSpan.Parse(request.ClosingTime);
                if (request.IsActive.HasValue)
                    existingShop.IsActive = request.IsActive.Value;
                if (request.IsDefault.HasValue)
                    existingShop.IsDefault = request.IsDefault.Value;
                    
                // Handle default shop business logic
                if (request.IsDefault.HasValue && request.IsDefault.Value)
                {
                    // If setting this shop as default, ensure no other shop is default for this owner
                    var otherShops = await _context.Shops
                        .Where(s => s.OwnerId == currentUserId.Value && s.Id != id && !s.IsDeleted)
                        .ToListAsync();
                    
                    foreach (var shop in otherShops)
                    {
                        shop.IsDefault = false;
                    }
                }
                    
                existingShop.UpdatedAt = DateTime.UtcNow;
                
                // Update location if provided
                if (request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    existingShop.Latitude = request.Latitude.Value;
                    existingShop.Longitude = request.Longitude.Value;
                }
                
                // Update business hours if provided
                if (request.BusinessHours != null)
                {
                    existingShop.BusinessHours = JsonSerializer.Serialize(request.BusinessHours);
                }
                
                // Update profile picture URL only when explicitly provided
                if (request.ProfilePictureUrl != null)
                {
                    if (!string.IsNullOrEmpty(request.ProfilePictureUrl))
                    {
                        // Delete old profile picture if it exists and is different from new one
                        if (!string.IsNullOrEmpty(existingShop.ProfilePictureUrl) && 
                            existingShop.ProfilePictureUrl != request.ProfilePictureUrl)
                        {
                            try
                            {
                                _logger.LogInformation("Deleting old shop profile picture: {OldUrl}", existingShop.ProfilePictureUrl);
                                await _fileService.DeleteFileAsync(existingShop.ProfilePictureUrl, "shop-images");
                            }
                            catch (Exception deleteEx)
                            {
                                _logger.LogWarning(deleteEx, "Failed to delete old shop profile picture: {OldUrl}", existingShop.ProfilePictureUrl);
                                // Continue with update even if deletion fails
                            }
                        }
                        existingShop.ProfilePictureUrl = request.ProfilePictureUrl;
                    }
                    else if (request.ProfilePictureUrl == "")
                    {
                        // If ProfilePictureUrl is explicitly set to empty string, remove it
                        // Delete old profile picture if it exists
                        if (!string.IsNullOrEmpty(existingShop.ProfilePictureUrl))
                        {
                            try
                            {
                                _logger.LogInformation("Deleting shop profile picture due to removal: {OldUrl}", existingShop.ProfilePictureUrl);
                                await _fileService.DeleteFileAsync(existingShop.ProfilePictureUrl, "shop-images");
                            }
                            catch (Exception deleteEx)
                            {
                                _logger.LogWarning(deleteEx, "Failed to delete shop profile picture during removal: {OldUrl}", existingShop.ProfilePictureUrl);
                                // Continue with update even if deletion fails
                            }
                        }
                        existingShop.ProfilePictureUrl = null;
                    }
                }
                
                // Update image URLs only when explicitly provided
                if (request.ImageUrls != null)
                {
                    // Get current gallery images for deletion
                    List<string> currentImageUrls = new List<string>();
                    if (!string.IsNullOrEmpty(existingShop.ImageUrls))
                    {
                        try
                        {
                            currentImageUrls = JsonSerializer.Deserialize<List<string>>(existingShop.ImageUrls) ?? new List<string>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse existing ImageUrls for shop {ShopId}, starting with empty list", id);
                            currentImageUrls = new List<string>();
                        }
                    }

                    // Delete old gallery images that are not in the new list
                    var imagesToDelete = currentImageUrls.Where(oldUrl => !request.ImageUrls.Contains(oldUrl)).ToList();
                    if (imagesToDelete.Any())
                    {
                        try
                        {
                            _logger.LogInformation("Deleting {Count} old gallery images", imagesToDelete.Count);
                            await _fileService.DeleteMultipleFilesAsync(imagesToDelete, "shop-images");
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, "Failed to delete some old gallery images");
                            // Continue with update even if deletion fails
                        }
                    }

                    existingShop.ImageUrls = JsonSerializer.Serialize(request.ImageUrls);
                }

                await _context.SaveChangesAsync();

                var responseDto = new ShopResponseDto
                {
                    Id = existingShop.Id,
                    Name = existingShop.Name ?? "",
                    Description = existingShop.Description ?? "",
                    Address = existingShop.Address ?? "",
                    City = existingShop.City ?? "",
                    State = existingShop.State ?? "",
                    ZipCode = existingShop.ZipCode ?? "",
                    District = existingShop.District ?? "",
                    PhoneNumber = existingShop.PhoneNumber ?? "",
                    Email = existingShop.Email ?? "",
                    ServiceType = existingShop.ServiceType,
                    GenderServices = !string.IsNullOrEmpty(existingShop.GenderServices) 
                        ? JsonSerializer.Deserialize<List<string>>(existingShop.GenderServices) ?? new List<string>()
                        : new List<string>(),
                    Specializations = !string.IsNullOrEmpty(existingShop.Specializations) 
                        ? JsonSerializer.Deserialize<List<string>>(existingShop.Specializations) ?? new List<string>()
                        : new List<string>(),
                    // Bank Details
                    BankAccountNumber = existingShop.BankAccountNumber,
                    IFSCCode = existingShop.IFSCCode,
                    BankName = existingShop.BankName,
                    AccountHolderName = existingShop.AccountHolderName,
                    // Tax Details
                    GSTNumber = existingShop.GSTNumber,
                    PANNumber = existingShop.PANNumber,
                    OpeningTime = existingShop.OpeningTime,
                    ClosingTime = existingShop.ClosingTime,
                    BusinessHours = existingShop.BusinessHours,
                    Latitude = existingShop.Latitude,
                    Longitude = existingShop.Longitude,
                    IsActive = existingShop.IsActive,
                    IsDefault = existingShop.IsDefault,
                    OwnerId = existingShop.OwnerId,
                    CreatedAt = existingShop.CreatedAt,
                    UpdatedAt = existingShop.UpdatedAt,
                    ProfilePictureUrl = existingShop.ProfilePictureUrl ?? "",
                    ImageUrls = string.IsNullOrEmpty(existingShop.ImageUrls) ? 
                        new List<string>() : 
                        JsonSerializer.Deserialize<List<string>>(existingShop.ImageUrls) ?? new List<string>()
                };

                _logger.LogInformation($"✅ Shop updated successfully with ID: {existingShop.Id}");
                return Ok(ApiResponse<ShopResponseDto>.SuccessResponse(responseDto, "Shop updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shop");
                return StatusCode(500, ApiResponse<ShopResponseDto>.ErrorResponse("An error occurred while updating the shop"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteShop(int id)
        {
            try
            {
                _logger.LogInformation($"🗑️ DeleteShop called for shop ID: {id}");
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
                }

                // Find the existing shop
                var existingShop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == currentUserId);
                if (existingShop == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Shop not found or you don't have permission to delete it"));
                }

                // Clean up all shop images before deletion
                await CleanupShopImagesAsync(existingShop);

                // Remove the shop from the database
                _context.Shops.Remove(existingShop);
                await _context.SaveChangesAsync();

                // Auto-set default shop if only one remains
                await AutoSetDefaultShopIfNeeded(currentUserId.Value);

                _logger.LogInformation($"✅ Shop deleted successfully with ID: {id}");
                return Ok(ApiResponse<object>.SuccessResponse(new { }, "Shop deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shop");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting the shop"));
            }
        }

        [HttpPost("delete-with-otp")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteShopWithOtp([FromBody] ShopDeleteRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
                }

                var currentUserEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User email not found in token"));
                }

                // Verify email matches current user's email (owner email)
                if (!string.Equals(currentUserEmail, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Email does not match the shop owner's registered email"));
                }

                // Verify OTP first
                var otpResult = await _otpService.VerifyOtpAsync(request.Email, request.OtpCode, OtpEntity.PURPOSE_SHOP_DELETE);
                if (!otpResult.Success)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse($"OTP verification failed: {otpResult.Message}"));
                }

                // Find the shop
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == request.ShopId && s.OwnerId == currentUserId && !s.IsDeleted);
                if (shop == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Shop not found or you don't have permission to delete it"));
                }

                // Clean up all shop images before deletion
                await CleanupShopImagesAsync(shop);

                // Remove the shop from the database
                _context.Shops.Remove(shop);
                await _context.SaveChangesAsync();

                // Auto-set default shop if only one remains
                await AutoSetDefaultShopIfNeeded(currentUserId.Value);

                _logger.LogInformation($"✅ Shop deleted successfully with OTP verification. Shop ID: {request.ShopId}, Owner: {currentUserEmail}");
                return Ok(ApiResponse<object>.SuccessResponse(new { ShopId = request.ShopId, ShopName = shop.Name }, "Shop deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shop with OTP");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting the shop"));
            }
        }

        [HttpGet("check-shop-phone-status/{phone}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> CheckShopPhoneStatus(string phone)
        {
            try
            {
                if (string.IsNullOrEmpty(phone))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Phone number is required"));
                }

                // Clean the phone number to handle different formats
                var cleanPhone = Regex.Replace(phone, @"[^\d+]", "");

                var shop = await _context.Shops
                    .Where(s => s.PhoneNumber == cleanPhone && !s.IsDeleted)
                    .Select(s => new { s.PhoneNumber, s.Name, s.City, s.State })
                    .FirstOrDefaultAsync();

                if (shop == null)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { 
                        exists = false, 
                        status = "not_registered",
                        message = "Phone number is available for shop registration"
                    }, "Phone number not registered for any shop"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(new { 
                    exists = true, 
                    status = "registered",
                    shop = new {
                        phoneNumber = shop.PhoneNumber,
                        name = shop.Name,
                        location = $"{shop.City}, {shop.State}"
                    },
                    message = "Phone number is already registered for another shop"
                }, "Phone number is already registered for a shop"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking shop phone number status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while checking phone number status"));
            }
        }

        [HttpGet("check-shop-email-status/{email}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> CheckShopEmailStatus(string email)
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

                var shop = await _context.Shops
                    .Where(s => s.Email.ToLower() == email.ToLower() && !s.IsDeleted)
                    .Select(s => new { s.Email, s.Name, s.City, s.State })
                    .FirstOrDefaultAsync();

                if (shop == null)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { 
                        exists = false, 
                        status = "not_registered",
                        message = "Email address is available for shop registration"
                    }, "Email address not registered for any shop"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(new { 
                    exists = true, 
                    status = "registered",
                    shop = new {
                        email = shop.Email,
                        name = shop.Name,
                        location = $"{shop.City}, {shop.State}"
                    },
                    message = "Email address is already registered for another shop"
                }, "Email address is already registered for a shop"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking shop email address status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while checking email address status"));
            }
        }

        [HttpPut("{shopId}/gallery-images")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateShopGalleryImages(int shopId, [FromBody] UpdateShopGalleryImagesDto request)
        {
            try
            {
                _logger.LogInformation($"🖼️ UpdateShopGalleryImages called for shop ID: {shopId}");
                
                if (request == null || request.ImageUrls == null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data"));
                }
                
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
                }

                // Find the existing shop
                var existingShop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == shopId && s.OwnerId == currentUserId && !s.IsDeleted);
                if (existingShop == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Shop not found or you don't have permission to update it"));
                }

                // Get current gallery images for deletion
                List<string> currentImageUrls = new List<string>();
                if (!string.IsNullOrEmpty(existingShop.ImageUrls))
                {
                    try
                    {
                        currentImageUrls = JsonSerializer.Deserialize<List<string>>(existingShop.ImageUrls) ?? new List<string>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse existing ImageUrls for shop {ShopId}, starting with empty list", shopId);
                        currentImageUrls = new List<string>();
                    }
                }

                // Determine which images to delete (old images not in new list)
                var imagesToDelete = currentImageUrls.Where(oldUrl => !request.ImageUrls.Contains(oldUrl)).ToList();
                
                // Delete old images that are no longer needed
                if (imagesToDelete.Any())
                {
                    try
                    {
                        _logger.LogInformation("Deleting {Count} old gallery images for shop {ShopId}", imagesToDelete.Count, shopId);
                        await _fileService.DeleteMultipleFilesAsync(imagesToDelete, "shop-images");
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete some old gallery images for shop {ShopId}", shopId);
                        // Continue with update even if deletion fails
                    }
                }

                // Update shop with new gallery images
                existingShop.ImageUrls = JsonSerializer.Serialize(request.ImageUrls);
                
                // Update profile picture if specified or set to first gallery image if profile is empty
                if (!string.IsNullOrEmpty(request.ProfilePictureUrl))
                {
                    // Only update if the new profile picture is different from current
                    if (existingShop.ProfilePictureUrl != request.ProfilePictureUrl)
                    {
                        // Delete old profile picture if it exists and is not in the new gallery
                        if (!string.IsNullOrEmpty(existingShop.ProfilePictureUrl) && 
                            !request.ImageUrls.Contains(existingShop.ProfilePictureUrl))
                        {
                            try
                            {
                                _logger.LogInformation("Deleting old profile picture for shop {ShopId}: {OldUrl}", shopId, existingShop.ProfilePictureUrl);
                                await _fileService.DeleteFileAsync(existingShop.ProfilePictureUrl, "shop-images");
                            }
                            catch (Exception deleteEx)
                            {
                                _logger.LogWarning(deleteEx, "Failed to delete old profile picture for shop {ShopId}", shopId);
                            }
                        }
                        existingShop.ProfilePictureUrl = request.ProfilePictureUrl;
                    }
                }
                else if (string.IsNullOrEmpty(existingShop.ProfilePictureUrl) && request.ImageUrls.Any())
                {
                    // Set first gallery image as profile picture if no profile picture exists
                    existingShop.ProfilePictureUrl = request.ImageUrls.First();
                }
                
                existingShop.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var response = new
                {
                    shopId = existingShop.Id,
                    profilePictureUrl = existingShop.ProfilePictureUrl,
                    galleryImages = request.ImageUrls,
                    deletedImagesCount = imagesToDelete.Count,
                    totalGalleryImages = request.ImageUrls.Count
                };

                _logger.LogInformation($"✅ Shop gallery images updated successfully for shop ID: {shopId}");
                return Ok(ApiResponse<object>.SuccessResponse(response, "Shop gallery images updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shop gallery images for shop {ShopId}", shopId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while updating shop gallery images"));
            }
        }

        [HttpDelete("{shopId}/images/{imageUrl}")]
        [Authorize(Roles = "ShopOwner,Admin")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteShopImage(int shopId, string imageUrl)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token"));
                }

                // URL decode the image URL parameter
                imageUrl = Uri.UnescapeDataString(imageUrl);
                
                _logger.LogInformation($"🗑️ DeleteShopImage called for shop ID: {shopId}, imageUrl: {imageUrl}");

                var shop = await _context.Shops
                    .FirstOrDefaultAsync(s => s.Id == shopId && !s.IsDeleted);

                if (shop == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Shop not found"));
                }

                // Check ownership (only owner or admin can delete images)
                if (userRole != "Admin" && shop.OwnerId != currentUserId.Value)
                {
                    return Forbid("You can only delete images from your own shop");
                }

                // Get current images
                var currentImages = !string.IsNullOrEmpty(shop.ImageUrls) 
                    ? JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>()
                    : new List<string>();

                // Check if the image exists in the shop's image list
                var imageToDelete = currentImages.FirstOrDefault(img => img == imageUrl);
                if (imageToDelete == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Image not found in shop's gallery"));
                }

                // Remove image from the list
                currentImages.Remove(imageToDelete);

                // Update shop's image URLs
                shop.ImageUrls = currentImages.Any() 
                    ? JsonSerializer.Serialize(currentImages)
                    : null;

                // If the deleted image was the profile picture, set a new one or clear it
                if (shop.ProfilePictureUrl == imageUrl)
                {
                    shop.ProfilePictureUrl = currentImages.FirstOrDefault();
                }

                shop.UpdatedAt = DateTime.UtcNow;

                // Save changes to database
                await _context.SaveChangesAsync();

                // Delete the file from Azure Blob Storage
                try
                {
                    await _fileService.DeleteFileAsync(imageUrl, "shop-images");
                    _logger.LogInformation("✅ File deleted from Azure Blob Storage: {ImageUrl}", imageUrl);
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "❌ Error deleting file from Azure Blob Storage: {ImageUrl}", imageUrl);
                    // Continue execution even if physical file deletion fails
                }

                _logger.LogInformation($"✅ Image deleted successfully from shop {shopId}");

                return Ok(ApiResponse<object>.SuccessResponse(new 
                { 
                    deletedImageUrl = imageUrl,
                    remainingImagesCount = currentImages.Count,
                    newProfilePictureUrl = shop.ProfilePictureUrl
                }, "Image deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting shop image: {imageUrl}");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting the image"));
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

        /// <summary>
        /// Automatically sets the default shop if there's only one shop left
        /// </summary>
        private async Task AutoSetDefaultShopIfNeeded(int ownerId)
        {
            try
            {
                var userShops = await _context.Shops
                    .Where(s => s.OwnerId == ownerId && !s.IsDeleted)
                    .ToListAsync();

                // If there's exactly one shop and it's not set as default, make it default
                if (userShops.Count == 1 && !userShops[0].IsDefault)
                {
                    userShops[0].IsDefault = true;
                    userShops[0].UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"🎯 Auto-set shop '{userShops[0].Name}' as default (only one shop remaining)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-setting default shop");
            }
        }

        /// <summary>
        /// Cleans up all images associated with a shop (profile picture and gallery images)
        /// </summary>
        private async Task CleanupShopImagesAsync(Shop shop)
        {
            try
            {
                _logger.LogInformation("🧹 Starting cleanup of all images for shop {ShopId}: {ShopName}", shop.Id, shop.Name);

                var imagesToDelete = new List<string>();

                // Add profile picture if exists
                if (!string.IsNullOrEmpty(shop.ProfilePictureUrl))
                {
                    imagesToDelete.Add(shop.ProfilePictureUrl);
                    _logger.LogInformation("Added profile picture to deletion list: {ProfileUrl}", shop.ProfilePictureUrl);
                }

                // Add gallery images if they exist
                if (!string.IsNullOrEmpty(shop.ImageUrls))
                {
                    try
                    {
                        var galleryImages = JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>();
                        imagesToDelete.AddRange(galleryImages.Where(url => !string.IsNullOrEmpty(url)));
                        _logger.LogInformation("Added {Count} gallery images to deletion list", galleryImages.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse gallery images for shop {ShopId}, skipping gallery cleanup", shop.Id);
                    }
                }

                // Delete all images in batch
                if (imagesToDelete.Any())
                {
                    _logger.LogInformation("Deleting {Count} total images for shop {ShopId}", imagesToDelete.Count, shop.Id);
                    await _fileService.DeleteMultipleFilesAsync(imagesToDelete, "shop-images");
                    _logger.LogInformation("✅ Successfully cleaned up all images for shop {ShopId}", shop.Id);
                }
                else
                {
                    _logger.LogInformation("No images to cleanup for shop {ShopId}", shop.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during image cleanup for shop {ShopId}: {ShopName}", shop.Id, shop.Name);
                // Don't throw exception as we want shop deletion to continue even if image cleanup fails
            }
        }

        private async Task DeleteOldShopImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogWarning("Cannot delete shop image: URL is null or empty");
                    return;
                }

                _logger.LogInformation("Deleting old shop image: {ImageUrl}", imageUrl);
                
                // Use the IFileService to delete from Azure Blob Storage
                await _fileService.DeleteFileAsync(imageUrl, "shop-images");
                
                _logger.LogInformation("Old shop image deleted successfully from Azure: {ImageUrl}", imageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old shop image: {ImageUrl}", imageUrl);
                throw; // Re-throw to let the caller handle it
            }
        }
    }
}

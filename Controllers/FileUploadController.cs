using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Services.Interfaces;
using System.Security.Claims;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FileUploadController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public FileUploadController(IFileService fileService, ILogger<FileUploadController> logger, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _fileService = fileService;
            _logger = logger;
            _environment = environment;
            _context = context;
        }

        [HttpPost("profile-image")]
        [Authorize]
        public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile file)
        {
            try
            {
                _logger.LogInformation("=== FILE UPLOAD CONTROLLER - PROFILE IMAGE UPLOAD STARTED ===");
                _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Request Content-Length: {ContentLength}", Request.ContentLength);
                _logger.LogInformation("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("Profile image upload failed: Invalid token");
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                _logger.LogInformation("Profile image upload for user ID: {UserId}", userId);

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    _logger.LogWarning("Profile image upload failed: User not found for ID {UserId}", userId);
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("File upload failed: No file provided");
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                _logger.LogInformation("File details - Name: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}", 
                    file.FileName, file.Length, file.ContentType);

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    _logger.LogWarning("File upload failed: Invalid file type {ContentType}", file.ContentType);
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed." 
                    });
                }

                // Validate file size (5MB max)
                const int maxFileSize = 5 * 1024 * 1024; // 5MB
                if (file.Length > maxFileSize)
                {
                    _logger.LogWarning("File upload failed: File size {FileSize} exceeds limit {MaxSize}", file.Length, maxFileSize);
                    return BadRequest(new { 
                        success = false, 
                        message = "File size exceeds 5MB limit" 
                    });
                }

                _logger.LogInformation("Starting file upload via FileService with old image deletion...");
                var oldProfileImageUrl = user.ProfilePictureUrl;
                var fileUrl = await _fileService.UpdateProfileImageAsync(file, oldProfileImageUrl, "profile-images");

                if (string.IsNullOrEmpty(fileUrl))
                {
                    _logger.LogError("File upload failed: FileService returned empty URL");
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Failed to upload file" 
                    });
                }

                // Update user profile picture URL in database
                _logger.LogInformation("Updating user profile image URL in database...");
                user.ProfilePictureUrl = fileUrl;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Database updated successfully");

                _logger.LogInformation("Profile image uploaded successfully: {FileUrl}", fileUrl);

                return Ok(new { 
                    success = true, 
                    message = "File uploaded successfully",
                    data = new { fileUrl }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== FILE UPLOAD CONTROLLER - ERROR === Exception during profile image upload: {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to upload file" 
                });
            }
        }

        [HttpPost("staff-image")]
        [Authorize]
        public async Task<IActionResult> UploadStaffImage([FromForm] IFormFile file, [FromForm] int staffId)
        {
            try
            {
                _logger.LogInformation("=== FILE UPLOAD CONTROLLER - STAFF IMAGE UPLOAD STARTED ===");
                _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Request Content-Length: {ContentLength}", Request.ContentLength);
                _logger.LogInformation("Staff ID: {StaffId}", staffId);
                
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("Staff image upload failed: Invalid token");
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                // Find the staff member and verify ownership/permission
                var staff = await _context.Staff
                    .Include(s => s.Shop)
                    .FirstOrDefaultAsync(s => s.Id == staffId);

                if (staff == null)
                {
                    _logger.LogWarning("Staff image upload failed: Staff not found for ID {StaffId}", staffId);
                    return NotFound(new { success = false, message = "Staff member not found" });
                }

                // Check if the current user is the shop owner or the staff member themselves
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return Unauthorized(new { success = false, message = "User not found" });
                }

                bool hasPermission = staff.UserId == userId || // Staff member updating their own photo
                                   staff.Shop.OwnerId == userId || // Shop owner updating staff photo
                                   user.Role == "Admin" || user.Role == "SuperAdmin"; // Admin roles

                if (!hasPermission)
                {
                    _logger.LogWarning("Staff image upload failed: User {UserId} doesn't have permission for staff {StaffId}", userId, staffId);
                    return Forbid();
                }

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("File upload failed: No file provided");
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                _logger.LogInformation("File details - Name: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}", 
                    file.FileName, file.Length, file.ContentType);

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    _logger.LogWarning("File upload failed: Invalid file type {ContentType}", file.ContentType);
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed." 
                    });
                }

                // Validate file size (5MB max)
                const int maxFileSize = 5 * 1024 * 1024; // 5MB
                if (file.Length > maxFileSize)
                {
                    _logger.LogWarning("File upload failed: File size {FileSize} exceeds limit {MaxSize}", file.Length, maxFileSize);
                    return BadRequest(new { 
                        success = false, 
                        message = "File size exceeds 5MB limit" 
                    });
                }

                _logger.LogInformation("Starting staff image upload via FileService with old image deletion...");
                var oldPhotoUrl = staff.PhotoUrl;
                var fileUrl = await _fileService.UpdateProfileImageAsync(file, oldPhotoUrl, "staff-images");

                if (string.IsNullOrEmpty(fileUrl))
                {
                    _logger.LogError("File upload failed: FileService returned empty URL");
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Failed to upload file" 
                    });
                }

                // Update staff photo URL in database
                _logger.LogInformation("Updating staff photo URL in database...");
                staff.PhotoUrl = fileUrl;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Database updated successfully");

                _logger.LogInformation("Staff image uploaded successfully: {FileUrl}", fileUrl);

                return Ok(new { 
                    success = true, 
                    message = "Staff image uploaded successfully",
                    data = new { fileUrl }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== FILE UPLOAD CONTROLLER - ERROR === Exception during staff image upload: {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to upload file" 
                });
            }
        }

        [HttpPost("service-image")]
        [Authorize]
        public async Task<IActionResult> UploadServiceImage([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed." 
                    });
                }

                // Validate file size (10MB max for service images)
                const int maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "File size exceeds 10MB limit" 
                    });
                }

                var fileUrl = await _fileService.UploadFileAsync(file, "service-images");

                if (string.IsNullOrEmpty(fileUrl))
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Failed to upload file" 
                    });
                }

                _logger.LogInformation("Service image uploaded successfully: {FileUrl}", fileUrl);

                return Ok(new { 
                    success = true, 
                    message = "File uploaded successfully",
                    data = new { fileUrl }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading service image");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error" 
                });
            }
        }

        [HttpPost("shop-image")]
        [Authorize]
        public async Task<IActionResult> UploadShopImage([FromForm] IFormFile file, [FromForm] int shopId, [FromForm] bool isProfileImage = true)
        {
            try
            {
                _logger.LogInformation("=== FILE UPLOAD CONTROLLER - SHOP IMAGE UPLOAD STARTED ===");
                _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Request Content-Length: {ContentLength}", Request.ContentLength);
                _logger.LogInformation("Shop ID: {ShopId}, IsProfileImage: {IsProfileImage}", shopId, isProfileImage);
                
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("Shop image upload failed: Invalid token");
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                // Find the shop and verify ownership/permission
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == shopId);
                if (shop == null)
                {
                    _logger.LogWarning("Shop image upload failed: Shop not found for ID {ShopId}", shopId);
                    return NotFound(new { success = false, message = "Shop not found" });
                }

                // Check if the current user is the shop owner or admin
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return Unauthorized(new { success = false, message = "User not found" });
                }

                bool hasPermission = shop.OwnerId == userId || // Shop owner
                                   user.Role == "Admin" || user.Role == "SuperAdmin"; // Admin roles

                if (!hasPermission)
                {
                    _logger.LogWarning("Shop image upload failed: User {UserId} doesn't have permission for shop {ShopId}", userId, shopId);
                    return Forbid();
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                _logger.LogInformation("File details - Name: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}", 
                    file.FileName, file.Length, file.ContentType);

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed." 
                    });
                }

                // Validate file size (10MB max)
                const int maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "File size exceeds 10MB limit" 
                    });
                }

                string? oldImageUrl = null;
                if (isProfileImage)
                {
                    oldImageUrl = shop.ProfilePictureUrl;
                    _logger.LogInformation("Updating shop profile image. Old URL: {OldUrl}", oldImageUrl);
                }
                else
                {
                    _logger.LogInformation("Adding new gallery image to shop");
                }

                _logger.LogInformation("Starting shop image upload via FileService with old image deletion...");
                var fileUrl = await _fileService.UpdateShopImageAsync(file, oldImageUrl, "shop-images", isProfileImage);

                if (string.IsNullOrEmpty(fileUrl))
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Failed to upload file" 
                    });
                }

                // Update shop image URL in database
                _logger.LogInformation("Updating shop image URL in database...");
                if (isProfileImage)
                {
                    shop.ProfilePictureUrl = fileUrl;
                }
                else
                {
                    // Add to gallery images (ImageUrls is a JSON array as string)
                    var imageUrls = new List<string>();
                    if (!string.IsNullOrEmpty(shop.ImageUrls))
                    {
                        try
                        {
                            imageUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(shop.ImageUrls) ?? new List<string>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse existing ImageUrls, starting with empty list");
                            imageUrls = new List<string>();
                        }
                    }
                    imageUrls.Add(fileUrl);
                    shop.ImageUrls = System.Text.Json.JsonSerializer.Serialize(imageUrls);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Database updated successfully");

                _logger.LogInformation("Shop image uploaded successfully: {FileUrl}", fileUrl);

                return Ok(new { 
                    success = true, 
                    message = "Shop image uploaded successfully",
                    data = new { 
                        fileUrl,
                        isProfileImage,
                        shopId 
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== FILE UPLOAD CONTROLLER - ERROR === Exception during shop image upload: {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Failed to upload file" 
                });
            }
        }

        [HttpGet("list/{containerName}")]
        [Authorize]
        public IActionResult ListFiles(string containerName)
        {
            try
            {
                var allowedContainers = new[] { "profile-images", "service-images", "shop-images", "staff-images", "product-images" };
                if (!allowedContainers.Contains(containerName))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid container name" 
                    });
                }

                // This is a basic implementation - you might want to enhance this
                // by adding database tracking of uploaded files
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", containerName);
                
                if (!Directory.Exists(uploadsPath))
                {
                    return Ok(new { 
                        success = true, 
                        data = new string[0] 
                    });
                }

                var files = Directory.GetFiles(uploadsPath)
                    .Select(f => $"/uploads/{containerName}/{Path.GetFileName(f)}")
                    .Where(f => !f.EndsWith(".txt")) // Exclude test files
                    .ToArray();

                return Ok(new { 
                    success = true, 
                    data = files 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files in container {ContainerName}", containerName);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error" 
                });
            }
        }

        [HttpGet("test-upload-config")]
        public IActionResult TestUploadConfiguration()
        {
            try
            {
                var result = new
                {
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    WebRootPath = _environment.WebRootPath,
                    ContentRootPath = _environment.ContentRootPath,
                    UploadsPath = Path.Combine(_environment.WebRootPath ?? "", "uploads"),
                    ProfileImagesPath = Path.Combine(_environment.WebRootPath ?? "", "uploads", "profile-images"),
                    DirectoryExists = new
                    {
                        WebRoot = Directory.Exists(_environment.WebRootPath ?? ""),
                        Uploads = Directory.Exists(Path.Combine(_environment.WebRootPath ?? "", "uploads")),
                        ProfileImages = Directory.Exists(Path.Combine(_environment.WebRootPath ?? "", "uploads", "profile-images"))
                    },
                    FileServiceType = _fileService.GetType().Name,
                    Timestamp = DateTime.UtcNow
                };
                
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upload configuration");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error getting upload configuration",
                    error = ex.Message 
                });
            }
        }

        [HttpDelete("{containerName}/{fileName}")]
        [Authorize]
        public async Task<IActionResult> DeleteFile(string containerName, string fileName)
        {
            try
            {
                var allowedContainers = new[] { "profile-images", "service-images", "shop-images", "staff-images", "product-images" };
                if (!allowedContainers.Contains(containerName))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid container name" 
                    });
                }

                var fileUrl = $"/uploads/{containerName}/{fileName}";
                await _fileService.DeleteFileAsync(fileUrl, containerName);

                _logger.LogInformation("File deleted successfully: {FileUrl}", fileUrl);

                return Ok(new { 
                    success = true, 
                    message = "File deleted successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName} from container {ContainerName}", fileName, containerName);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error" 
                });
            }
        }

        private int? GetCurrentUserId()
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return null;
            }

            // Get user id from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}

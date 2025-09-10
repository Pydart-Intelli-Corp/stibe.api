using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stibe.api.Services.Interfaces;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(IFileService fileService, ILogger<FileUploadController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        [HttpPost("profile-image")]
        [Authorize]
        public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile file)
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

                // Validate file size (5MB max)
                const int maxFileSize = 5 * 1024 * 1024; // 5MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "File size exceeds 5MB limit" 
                    });
                }

                var fileUrl = await _fileService.UploadFileAsync(file, "profile-images");

                if (string.IsNullOrEmpty(fileUrl))
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Failed to upload file" 
                    });
                }

                _logger.LogInformation("Profile image uploaded successfully: {FileUrl}", fileUrl);

                return Ok(new { 
                    success = true, 
                    message = "File uploaded successfully",
                    data = new { fileUrl }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error" 
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
        public async Task<IActionResult> UploadShopImage([FromForm] IFormFile file)
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

                // Validate file size (10MB max)
                const int maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "File size exceeds 10MB limit" 
                    });
                }

                var fileUrl = await _fileService.UploadFileAsync(file, "shop-images");

                if (string.IsNullOrEmpty(fileUrl))
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Failed to upload file" 
                    });
                }

                _logger.LogInformation("Shop image uploaded successfully: {FileUrl}", fileUrl);

                return Ok(new { 
                    success = true, 
                    message = "File uploaded successfully",
                    data = new { fileUrl }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading shop image");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Internal server error" 
                });
            }
        }

        [HttpGet("list/{containerName}")]
        [Authorize]
        public IActionResult ListFiles(string containerName)
        {
            try
            {
                var allowedContainers = new[] { "profile-images", "service-images", "shop-images", "product-images" };
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

        [HttpDelete("{containerName}/{fileName}")]
        [Authorize]
        public async Task<IActionResult> DeleteFile(string containerName, string fileName)
        {
            try
            {
                var allowedContainers = new[] { "profile-images", "service-images", "shop-images", "product-images" };
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
    }
}

// Controllers/FileStorageTestController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using stibe.api.Services.Interfaces;
using stibe.api.Models.DTOs.Features;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileStorageTestController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileStorageTestController> _logger;

        public FileStorageTestController(
            IFileService fileService,
            IConfiguration configuration,
            ILogger<FileStorageTestController> logger)
        {
            _fileService = fileService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get current file storage configuration and status
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous]
        public IActionResult GetFileStorageStatus()
        {
            var provider = _configuration["FileStorage:Provider"] ?? "local";
            object config = provider.ToLowerInvariant() switch
            {
                "azure" => new
                {
                    CurrentProvider = provider.ToUpperInvariant(),
                    Provider = "Azure Blob Storage",
                    ContainerName = _configuration["FileStorage:Azure:ContainerName"],
                    BaseUrl = _configuration["FileStorage:Azure:BaseUrl"],
                    HasConnectionString = !string.IsNullOrEmpty(_configuration["FileStorage:Azure:ConnectionString"])
                },
                "local" => new
                {
                    CurrentProvider = provider.ToUpperInvariant(),
                    Provider = "Local File System",
                    LocalPath = _configuration["FileStorage:LocalPath"],
                    BaseUrl = _configuration["FileStorage:BaseUrl"]
                },
                "hybrid" => new
                {
                    CurrentProvider = provider.ToUpperInvariant(),
                    Provider = "Hybrid (Azure with Local Fallback)",
                    PrimaryProvider = "Azure Blob Storage",
                    FallbackProvider = "Local File System"
                },
                _ => new
                {
                    CurrentProvider = provider.ToUpperInvariant(),
                    Provider = "Unknown",
                    Note = "Invalid provider configuration"
                }
            };

            var result = new
            {
                Configuration = config,
                AvailableProviders = new[] { "Local", "Azure", "Hybrid" },
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.SuccessResponse(result, "File storage status retrieved successfully"));
        }

        /// <summary>
        /// Test file upload with current provider
        /// </summary>
        [HttpPost("test-upload")]
        [AllowAnonymous]
        public async Task<IActionResult> TestFileUpload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));
            }

            try
            {
                var provider = _configuration["FileStorage:Provider"] ?? "local";
                _logger.LogInformation("Testing file upload with {Provider} provider", provider.ToUpperInvariant());

                var startTime = DateTime.UtcNow;
                var fileUrl = await _fileService.UploadFileAsync(file, "test-uploads");
                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalMilliseconds;

                var result = new
                {
                    Success = !string.IsNullOrEmpty(fileUrl),
                    FileUrl = fileUrl,
                    Provider = provider.ToUpperInvariant(),
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    UploadDurationMs = Math.Round(duration, 2),
                    Container = "test-uploads",
                    Timestamp = endTime
                };

                if (string.IsNullOrEmpty(fileUrl))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("File upload failed"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(result, "File uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File upload test failed");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Upload failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Test multiple file uploads
        /// </summary>
        [HttpPost("test-batch-upload")]
        public async Task<IActionResult> TestBatchUpload([FromForm] List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("No files provided"));
            }

            try
            {
                var provider = _configuration["FileStorage:Provider"] ?? "local";
                _logger.LogInformation("Testing batch upload of {Count} files with {Provider} provider", files.Count, provider.ToUpperInvariant());

                var startTime = DateTime.UtcNow;
                var fileUrls = await _fileService.UploadFilesAsync(files, "test-batch-uploads");
                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalMilliseconds;

                var result = new
                {
                    Success = fileUrls.Any(),
                    Provider = provider.ToUpperInvariant(),
                    FilesUploaded = fileUrls.Count,
                    TotalFiles = files.Count,
                    UploadDurationMs = Math.Round(duration, 2),
                    Container = "test-batch-uploads",
                    Files = files.Select((file, index) => new
                    {
                        OriginalFileName = file.FileName,
                        FileSize = file.Length,
                        ContentType = file.ContentType,
                        UploadedUrl = index < fileUrls.Count ? fileUrls[index] : null,
                        Status = index < fileUrls.Count ? "Success" : "Failed"
                    }).ToArray(),
                    Timestamp = endTime
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, $"Batch upload completed: {fileUrls.Count}/{files.Count} files uploaded"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch upload test failed");
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Batch upload failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Test file deletion
        /// </summary>
        [HttpDelete("test-delete")]
        public async Task<IActionResult> TestFileDelete([FromQuery] string fileUrl, [FromQuery] string container = "test-uploads")
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("File URL is required"));
            }

            try
            {
                var provider = _configuration["FileStorage:Provider"] ?? "local";
                _logger.LogInformation("Testing file deletion with {Provider} provider", provider.ToUpperInvariant());

                var startTime = DateTime.UtcNow;
                await _fileService.DeleteFileAsync(fileUrl, container);
                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalMilliseconds;

                var result = new
                {
                    Success = true,
                    Provider = provider.ToUpperInvariant(),
                    DeletedFileUrl = fileUrl,
                    Container = container,
                    DeleteDurationMs = Math.Round(duration, 2),
                    Timestamp = endTime
                };

                return Ok(ApiResponse<object>.SuccessResponse(result, "File deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File deletion test failed for URL: {FileUrl}", fileUrl);
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"File deletion failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Switch file storage provider (for testing purposes)
        /// </summary>
        [HttpPost("switch-provider")]
        public IActionResult SwitchProvider([FromBody] SwitchProviderRequest request)
        {
            if (string.IsNullOrEmpty(request.Provider))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Provider is required"));
            }

            var validProviders = new[] { "local", "azure", "hybrid" };
            if (!validProviders.Contains(request.Provider.ToLowerInvariant()))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse($"Invalid provider. Valid options: {string.Join(", ", validProviders)}"));
            }

            // Note: This would typically require application restart or dependency injection reconfiguration
            // This endpoint is for documentation purposes
            var result = new
            {
                CurrentProvider = _configuration["FileStorage:Provider"] ?? "local",
                RequestedProvider = request.Provider.ToLowerInvariant(),
                Note = "Provider switching requires application restart or dependency injection reconfiguration",
                RestartRequired = true,
                ConfigurationPath = "FileStorage:Provider"
            };

            return Ok(ApiResponse<object>.SuccessResponse(result, "Provider switch noted (restart required)"));
        }
    }

    public class SwitchProviderRequest
    {
        public string Provider { get; set; } = string.Empty;
    }
}
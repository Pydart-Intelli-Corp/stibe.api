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
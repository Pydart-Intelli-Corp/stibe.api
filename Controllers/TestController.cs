using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Services.Interfaces;
using System.Security.Claims;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse>> HealthCheck()
        {
            try
            {
                // Test database connection
                var userCount = await _context.Users.CountAsync();

                return Ok(ApiResponse.SuccessResponse($"API is healthy. Database connected. Users count: {userCount}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResponse($"Health check failed: {ex.Message}"));
            }
        }

        [HttpGet("protected")]
        [Authorize]
        public ActionResult<ApiResponse> ProtectedEndpoint()
        {
            var userName = User.Identity?.Name ?? "Unknown";
            // 🔧 FIX: Use ClaimTypes.Role instead of "role"
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

            return Ok(ApiResponse.SuccessResponse($"Hello {userName}! Your role is: {role}"));
        }

        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public ActionResult<ApiResponse> AdminOnlyEndpoint()
        {
            var userName = User.Identity?.Name ?? "Unknown";
            return Ok(ApiResponse.SuccessResponse($"Hello Admin {userName}! This is an admin-only endpoint"));
        }

        [HttpGet("shop-owner")]
        [Authorize(Roles = "ShopOwner")]
        public ActionResult<ApiResponse> ShopOwnerEndpoint()
        {
            var userName = User.Identity?.Name ?? "Unknown";
            return Ok(ApiResponse.SuccessResponse($"Hello Shop Owner {userName}! This is a shop owner endpoint"));
        }

        [HttpGet("customer")]
        [Authorize(Roles = "Customer")]
        public ActionResult<ApiResponse> CustomerEndpoint()
        {
            var userName = User.Identity?.Name ?? "Unknown";
            return Ok(ApiResponse.SuccessResponse($"Hello Customer {userName}! This is a customer endpoint"));
        }
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail([FromServices] IEmailService emailService)
        {
            var result = await emailService.SendEmailAsync("info.pydart@gmail.com", "Test", "This is a test email.");
            return Ok(result ? "Email sent successfully" : "Email failed");
        }

        [HttpGet("debug-claims")]
        [Authorize]
        public ActionResult<ApiResponse> DebugClaims()
        {
            var claims = User.Claims.Select(c => new {
                Type = c.Type,
                Value = c.Value
            }).ToList();

            return Ok(ApiResponse.SuccessResponse(claims, "Current user claims"));
        }

        [HttpGet("file-system-check")]
        public async Task<ActionResult<ApiResponse>> FileSystemCheck([FromServices] IWebHostEnvironment environment)
        {
            try
            {
                var info = new
                {
                    Environment = environment.EnvironmentName,
                    ContentRootPath = environment.ContentRootPath,
                    WebRootPath = environment.WebRootPath,
                    WebRootPathExists = !string.IsNullOrEmpty(environment.WebRootPath) && Directory.Exists(environment.WebRootPath),
                    UploadsPath = !string.IsNullOrEmpty(environment.WebRootPath) ? Path.Combine(environment.WebRootPath, "uploads") : "WebRootPath is null",
                    UploadsExists = !string.IsNullOrEmpty(environment.WebRootPath) && Directory.Exists(Path.Combine(environment.WebRootPath, "uploads")),
                    ProfileImagesPath = !string.IsNullOrEmpty(environment.WebRootPath) ? Path.Combine(environment.WebRootPath, "uploads", "profile-images") : "WebRootPath is null",
                    ProfileImagesExists = !string.IsNullOrEmpty(environment.WebRootPath) && Directory.Exists(Path.Combine(environment.WebRootPath, "uploads", "profile-images")),
                    CanCreateTestFile = false,
                    TestFileError = ""
                };

                // Test file creation
                if (!string.IsNullOrEmpty(environment.WebRootPath))
                {
                    try
                    {
                        var testDir = Path.Combine(environment.WebRootPath, "uploads", "profile-images");
                        Directory.CreateDirectory(testDir);
                        
                        var testFilePath = Path.Combine(testDir, "test_file.txt");
                        await System.IO.File.WriteAllTextAsync(testFilePath, "Test file content");
                        
                        if (System.IO.File.Exists(testFilePath))
                        {
                            System.IO.File.Delete(testFilePath);
                            info = info with { CanCreateTestFile = true };
                        }
                    }
                    catch (Exception ex)
                    {
                        info = info with { TestFileError = ex.Message };
                    }
                }

                return Ok(ApiResponse.SuccessResponse(info, "File system diagnostic"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResponse($"File system check failed: {ex.Message}"));
            }
        }
    }
}
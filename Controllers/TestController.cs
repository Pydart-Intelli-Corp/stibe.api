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

        [HttpGet("filesystem")]
        public ActionResult<ApiResponse> FileSystemCheck()
        {
            try
            {
                var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                
                var info = new
                {
                    WebRootPath = environment.WebRootPath,
                    ContentRootPath = environment.ContentRootPath,
                    EnvironmentName = environment.EnvironmentName,
                    WebRootExists = !string.IsNullOrEmpty(environment.WebRootPath) && Directory.Exists(environment.WebRootPath),
                    ContentRootExists = Directory.Exists(environment.ContentRootPath),
                    CurrentDirectory = Directory.GetCurrentDirectory(),
                    UploadsPath = !string.IsNullOrEmpty(environment.WebRootPath) ? 
                        Path.Combine(environment.WebRootPath, "uploads", "profile-images") : "WebRootPath is null"
                };

                return Ok(ApiResponse.SuccessResponse(info, "Filesystem check completed"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse.ErrorResponse($"Filesystem check failed: {ex.Message}"));
            }
        }
    }
}
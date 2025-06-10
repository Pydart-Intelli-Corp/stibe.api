using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs;
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

        [HttpGet("salon-owner")]
        [Authorize(Roles = "SalonOwner")]
        public ActionResult<ApiResponse> SalonOwnerEndpoint()
        {
            var userName = User.Identity?.Name ?? "Unknown";
            return Ok(ApiResponse.SuccessResponse($"Hello Salon Owner {userName}! This is a salon owner endpoint"));
        }

        [HttpGet("customer")]
        [Authorize(Roles = "Customer")]
        public ActionResult<ApiResponse> CustomerEndpoint()
        {
            var userName = User.Identity?.Name ?? "Unknown";
            return Ok(ApiResponse.SuccessResponse($"Hello Customer {userName}! This is a customer endpoint"));
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
    }
}
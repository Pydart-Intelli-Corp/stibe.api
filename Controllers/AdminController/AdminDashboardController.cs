// Create new file: Controllers/AdminController/AdminDashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.Entities.PartnersEntity;
using System.Security.Claims;

namespace stibe.api.Controllers.AdminController
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            ApplicationDbContext context,
            ILogger<AdminDashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get system overview statistics
        [HttpGet("overview")]
        public async Task<ActionResult<ApiResponse<SystemOverviewDto>>> GetSystemOverview()
        {
            var currentUser = await GetCurrentAdminUser();
            if (currentUser == null)
            {
                return Unauthorized(ApiResponse<SystemOverviewDto>.ErrorResponse("User not authorized"));
            }

            var overview = new SystemOverviewDto
            {
                TotalUsers = await _context.Users.CountAsync(u => !u.IsDeleted),
                TotalSalons = await _context.Salons.CountAsync(s => !s.IsDeleted),
                TotalBookings = await _context.Bookings.CountAsync(b => !b.IsDeleted),
                TotalStaff = await _context.Staff.CountAsync(s => !s.IsDeleted),
                ActiveSalons = await _context.Salons.CountAsync(s => s.IsActive && !s.IsDeleted),
                BookingsToday = await _context.Bookings.CountAsync(b => b.BookingDate.Date == DateTime.Today && !b.IsDeleted),
                Revenue = await _context.Bookings
                    .Where(b => b.Status == "Completed" && !b.IsDeleted)
                    .SumAsync(b => b.TotalAmount),
                LastUpdated = DateTime.Now
            };

            return Ok(ApiResponse<SystemOverviewDto>.SuccessResponse(overview));
        }

        // Get all users with pagination
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<UserListItemDto>>>> GetUsers(
            int page = 1, int pageSize = 10, string searchTerm = "", string sortBy = "Id", bool ascending = true)
        {
            var currentUser = await GetCurrentAdminUser();
            if (currentUser == null || !currentUser.CanMonitorUsers)
            {
                return Unauthorized(ApiResponse<PaginatedResult<UserListItemDto>>.ErrorResponse("Not authorized to monitor users"));
            }

            var query = _context.Users.Where(u => !u.IsDeleted);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.PhoneNumber.Contains(searchTerm)
                );
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "firstname" => ascending ? query.OrderBy(u => u.FirstName) : query.OrderByDescending(u => u.FirstName),
                "lastname" => ascending ? query.OrderBy(u => u.LastName) : query.OrderByDescending(u => u.LastName),
                "email" => ascending ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                "role" => ascending ? query.OrderBy(u => u.Role) : query.OrderByDescending(u => u.Role),
                "createdat" => ascending ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt),
                _ => ascending ? query.OrderBy(u => u.Id) : query.OrderByDescending(u => u.Id)
            };

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListItemDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    IsEmailVerified = u.IsEmailVerified,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            var result = new PaginatedResult<UserListItemDto>
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<PaginatedResult<UserListItemDto>>.SuccessResponse(result));
        }

        // Helper method to get current admin user
        private async Task<User?> GetCurrentAdminUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return null;
            }

            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && (u.Role == "Admin" || u.Role == "SuperAdmin"));

            return adminUser;
        }
    }

    // DTOs for admin dashboard
    public class SystemOverviewDto
    {
        public int TotalUsers { get; set; }
        public int TotalSalons { get; set; }
        public int TotalBookings { get; set; }
        public int TotalStaff { get; set; }
        public int ActiveSalons { get; set; }
        public int BookingsToday { get; set; }
        public decimal Revenue { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class UserListItemDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

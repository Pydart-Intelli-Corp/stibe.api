using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs;
using stibe.api.Models.Entities;
using stibe.api.Services.Interfaces;
using System.Security.Claims;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(
            ApplicationDbContext context,
            IPasswordService passwordService,
            IEmailService emailService,
            ILogger<StaffController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("register")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<ActionResult<ApiResponse<StaffResponseDto>>> RegisterStaff(StaffRegistrationRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<StaffResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify salon ownership (if not admin)
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser?.Role != "Admin")
                {
                    var salon = await _context.Salons
                        .FirstOrDefaultAsync(s => s.Id == request.SalonId && s.OwnerId == currentUserId.Value);

                    if (salon == null)
                    {
                        return Forbid("You can only register staff for your own salon");
                    }
                }

                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(ApiResponse<StaffResponseDto>.ErrorResponse("Email already exists"));
                }

                // Validate schedule
                if (request.StartTime >= request.EndTime)
                {
                    return BadRequest(ApiResponse<StaffResponseDto>.ErrorResponse("Start time must be before end time"));
                }

                if (request.LunchBreakStart >= request.LunchBreakEnd ||
                    request.LunchBreakStart < request.StartTime ||
                    request.LunchBreakEnd > request.EndTime)
                {
                    return BadRequest(ApiResponse<StaffResponseDto>.ErrorResponse("Invalid lunch break times"));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create User account for staff
                    var user = new User
                    {
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Email = request.Email,
                        PhoneNumber = request.PhoneNumber,
                        PasswordHash = _passwordService.HashPassword(request.Password),
                        Role = "Staff",
                        SalonId = request.SalonId,
                        IsStaffActive = true,
                        StaffJoinDate = DateTime.UtcNow,
                        IsEmailVerified = false
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Create Staff profile
                    var staff = new Staff
                    {
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Email = request.Email,
                        PhoneNumber = request.PhoneNumber,
                        Role = request.Role,
                        Bio = request.Bio,
                        IsActive = true,
                        StartTime = request.StartTime,
                        EndTime = request.EndTime,
                        LunchBreakStart = request.LunchBreakStart,
                        LunchBreakEnd = request.LunchBreakEnd,
                        ExperienceYears = request.ExperienceYears,
                        HourlyRate = request.HourlyRate,
                        CommissionRate = request.CommissionRate,
                        EmploymentType = request.EmploymentType,
                        SalonId = request.SalonId,
                        Certifications = request.Certifications,
                        Languages = request.Languages,
                        InstagramHandle = request.InstagramHandle,
                        JoinDate = DateTime.UtcNow
                    };

                    _context.Staff.Add(staff);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Get salon info for welcome email
                    var salonInfo = await _context.Salons.FindAsync(request.SalonId);

                    // Send welcome email
                    var welcomeMessage = $@"
                        <h2>🎉 Welcome to {salonInfo?.Name ?? "the team"}!</h2>
                        <p>You've been registered as a <strong>{request.Role}</strong>.</p>
                        
                        <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                            <h3>📱 Your Login Details:</h3>
                            <p><strong>Email:</strong> {request.Email}</p>
                            <p><strong>Password:</strong> {request.Password}</p>
                            <p><em>Please change your password after first login.</em></p>
                        </div>
                        
                        <div style='background: #e3f2fd; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                            <h3>💼 Your Work Details:</h3>
                            <p><strong>Working Hours:</strong> {request.StartTime:hh\\:mm} - {request.EndTime:hh\\:mm}</p>
                            <p><strong>Lunch Break:</strong> {request.LunchBreakStart:hh\\:mm} - {request.LunchBreakEnd:hh\\:mm}</p>
                            <p><strong>Commission Rate:</strong> {request.CommissionRate}%</p>
                            <p><strong>Hourly Rate:</strong> ₹{request.HourlyRate:F2}</p>
                        </div>
                        
                        <p>Welcome to the team! 💫</p>
                    ";

                    await _emailService.SendEmailAsync(request.Email, "Welcome to the Team! 🎉", welcomeMessage);

                    // Get complete staff info for response
                    var completeStaff = await GetStaffByIdAsync(staff.Id);

                    _logger.LogInformation($"Staff member registered successfully: {request.Email} for salon {request.SalonId}");
                    return Ok(ApiResponse<StaffResponseDto>.SuccessResponse(completeStaff,
                        "Staff member registered successfully! Welcome email sent with login details."));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering staff member");
                return StatusCode(500, ApiResponse<StaffResponseDto>.ErrorResponse("An error occurred during staff registration"));
            }
        }

        [HttpGet("salon/{salonId}")]
        [Authorize(Roles = "SalonOwner,Admin")]
        public async Task<ActionResult<ApiResponse<StaffListResponseDto>>> GetSalonStaff(
            int salonId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<StaffListResponseDto>.ErrorResponse("Invalid token"));
                }

                // Verify salon ownership (if not admin)
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser?.Role != "Admin")
                {
                    var salon = await _context.Salons
                        .FirstOrDefaultAsync(s => s.Id == salonId && s.OwnerId == currentUserId.Value);

                    if (salon == null)
                    {
                        return Forbid("You can only view staff for your own salon");
                    }
                }

                var query = _context.Staff
                    .Include(s => s.Salon)
                    .Where(s => s.SalonId == salonId);

                if (!includeInactive)
                {
                    query = query.Where(s => s.IsActive);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var skip = (page - 1) * pageSize;

                var staff = await query
                    .OrderBy(s => s.FirstName)
                    .ThenBy(s => s.LastName)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                var staffDtos = staff.Select(MapToStaffResponse).ToList();

                var response = new StaffListResponseDto
                {
                    Staff = staffDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                };

                return Ok(ApiResponse<StaffListResponseDto>.SuccessResponse(response,
                    $"Found {totalCount} staff members"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting salon staff");
                return StatusCode(500, ApiResponse<StaffListResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<StaffResponseDto>>> GetStaff(int id)
        {
            try
            {
                var staff = await GetStaffByIdAsync(id);
                if (staff == null)
                {
                    return NotFound(ApiResponse<StaffResponseDto>.ErrorResponse("Staff member not found"));
                }

                return Ok(ApiResponse<StaffResponseDto>.SuccessResponse(staff));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff member");
                return StatusCode(500, ApiResponse<StaffResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        // Helper methods
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }

        private async Task<StaffResponseDto?> GetStaffByIdAsync(int staffId)
        {
            var staff = await _context.Staff
                .Include(s => s.Salon)
                .FirstOrDefaultAsync(s => s.Id == staffId);

            return staff != null ? MapToStaffResponse(staff) : null;
        }

        private StaffResponseDto MapToStaffResponse(Staff staff)
        {
            return new StaffResponseDto
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = staff.Email,
                PhoneNumber = staff.PhoneNumber,
                Role = staff.Role,
                Bio = staff.Bio,
                PhotoUrl = staff.PhotoUrl,
                IsActive = staff.IsActive,
                StartTime = staff.StartTime,
                EndTime = staff.EndTime,
                LunchBreakStart = staff.LunchBreakStart,
                LunchBreakEnd = staff.LunchBreakEnd,
                ExperienceYears = staff.ExperienceYears,
                HourlyRate = staff.HourlyRate,
                CommissionRate = staff.CommissionRate,
                EmploymentType = staff.EmploymentType,
                AverageRating = staff.AverageRating,
                TotalReviews = staff.TotalReviews,
                TotalServices = staff.TotalServices,
                SalonId = staff.SalonId,
                SalonName = staff.Salon.Name,
                Certifications = staff.Certifications,
                Languages = staff.Languages,
                InstagramHandle = staff.InstagramHandle,
                JoinDate = staff.JoinDate,
                CreatedAt = staff.CreatedAt,
                UpdatedAt = staff.UpdatedAt
            };
        }
    }
}
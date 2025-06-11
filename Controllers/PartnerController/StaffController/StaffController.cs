using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Auth;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs.StaffsDTOs;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Models.Entities.PartnersEntity.StaffEntity;
using stibe.api.Services.Interfaces;
using stibe.api.Services.Interfaces.Partner;
using System.Security.Claims;

namespace stibe.api.Controllers.PartnerController.StaffController
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(
            ApplicationDbContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            IEmailService emailService,
            ILogger<StaffController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
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
                        UserId = user.Id,
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

                    // Get salon info for both email and response (MOVED OUTSIDE try-catch)
                    var salonInfo = await _context.Salons.FindAsync(request.SalonId);

                    // Send welcome email (wrap in try-catch to prevent failure)
                    try
                    {
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
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send welcome email to {Email}", request.Email);
                        // Don't fail the registration if email fails
                    }

                    // Create simple response without complex navigation properties
                    var staffResponse = new StaffResponseDto
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
                        SalonName = salonInfo?.Name ?? "Unknown Salon", // Now salonInfo is available here
                        Certifications = staff.Certifications,
                        Languages = staff.Languages,
                        InstagramHandle = staff.InstagramHandle,
                        JoinDate = staff.JoinDate,
                        CreatedAt = staff.CreatedAt,
                        UpdatedAt = staff.UpdatedAt
                    };

                    _logger.LogInformation($"Staff member registered successfully: {request.Email} for salon {request.SalonId}");
                    return Ok(ApiResponse<StaffResponseDto>.SuccessResponse(staffResponse,
                        "Staff member registered successfully! Welcome email sent with login details."));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in staff registration transaction");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering staff member");
                return StatusCode(500, ApiResponse<StaffResponseDto>.ErrorResponse("An error occurred during staff registration"));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> StaffLogin(StaffLoginRequestDto request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.StaffProfile)
                        .ThenInclude(s => s.Salon)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == "Staff" && !u.IsDeleted);

                if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid email or password"));
                }

                if (!user.IsStaffActive || user.StaffProfile?.IsActive != true)
                {
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Staff account is inactive"));
                }

                var token = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(60);

                var response = new LoginResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = new UserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Role = user.Role,
                        IsEmailVerified = user.IsEmailVerified,
                        CreatedAt = user.CreatedAt
                    }
                };

                _logger.LogInformation($"Staff member logged in: {user.Email} at {user.StaffProfile?.Salon?.Name}");
                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response,
                    $"Welcome back! You're now logged in to {user.StaffProfile?.Salon?.Name ?? "your salon"}."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during staff login");
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse("An error occurred during login"));
            }
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "Staff")]
        public async Task<ActionResult<ApiResponse<StaffDashboardResponseDto>>> GetStaffDashboard()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<StaffDashboardResponseDto>.ErrorResponse("Invalid token"));
                }

                var staff = await _context.Staff
                    .Include(s => s.Salon)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId.Value);

                if (staff == null)
                {
                    return NotFound(ApiResponse<StaffDashboardResponseDto>.ErrorResponse("Staff profile not found"));
                }

                var dashboard = new StaffDashboardResponseDto
                {
                    Profile = await GetStaffProfileSummaryAsync(staff.Id),
                    TodayWork = await GetTodayWorkSummaryAsync(staff.Id),
                    TodayBookings = await GetTodayBookingsAsync(staff.Id),
                    Performance = await GetStaffPerformanceAsync(staff.Id),
                    Notifications = await GetStaffNotificationsAsync(staff.Id),
                    MonthlyTarget = await GetMonthlyTargetAsync(staff.Id)
                };

                return Ok(ApiResponse<StaffDashboardResponseDto>.SuccessResponse(dashboard,
                    $"Welcome back, {staff.FirstName}! Here's your dashboard for today."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff dashboard");
                return StatusCode(500, ApiResponse<StaffDashboardResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("profile")]
        [Authorize(Roles = "Staff")]
        public async Task<ActionResult<ApiResponse<StaffProfileResponseDto>>> GetMyProfile()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<StaffProfileResponseDto>.ErrorResponse("Invalid token"));
                }

                var staff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId.Value);

                if (staff == null)
                {
                    return NotFound(ApiResponse<StaffProfileResponseDto>.ErrorResponse("Staff profile not found"));
                }

                var profile = await GetStaffProfileAsync(staff.Id);
                return Ok(ApiResponse<StaffProfileResponseDto>.SuccessResponse(profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff profile");
                return StatusCode(500, ApiResponse<StaffProfileResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Staff")]
        public async Task<ActionResult<ApiResponse<StaffProfileResponseDto>>> UpdateMyProfile(UpdateStaffProfileDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<StaffProfileResponseDto>.ErrorResponse("Invalid token"));
                }

                var staff = await _context.Staff
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId.Value);

                if (staff == null)
                {
                    return NotFound(ApiResponse<StaffProfileResponseDto>.ErrorResponse("Staff profile not found"));
                }

                // Update staff profile
                if (!string.IsNullOrWhiteSpace(request.FirstName))
                {
                    staff.FirstName = request.FirstName;
                    if (staff.User != null) staff.User.FirstName = request.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName))
                {
                    staff.LastName = request.LastName;
                    if (staff.User != null) staff.User.LastName = request.LastName;
                }

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    staff.PhoneNumber = request.PhoneNumber;
                    if (staff.User != null) staff.User.PhoneNumber = request.PhoneNumber;
                }

                if (request.Bio != null)
                    staff.Bio = request.Bio;

                if (!string.IsNullOrWhiteSpace(request.PhotoUrl))
                    staff.PhotoUrl = request.PhotoUrl;

                if (request.StartTime.HasValue && request.EndTime.HasValue)
                {
                    if (request.StartTime >= request.EndTime)
                    {
                        return BadRequest(ApiResponse<StaffProfileResponseDto>.ErrorResponse("Start time must be before end time"));
                    }
                    staff.StartTime = request.StartTime.Value;
                    staff.EndTime = request.EndTime.Value;
                }

                if (request.LunchBreakStart.HasValue && request.LunchBreakEnd.HasValue)
                {
                    if (request.LunchBreakStart >= request.LunchBreakEnd)
                    {
                        return BadRequest(ApiResponse<StaffProfileResponseDto>.ErrorResponse("Lunch break start must be before end"));
                    }
                    staff.LunchBreakStart = request.LunchBreakStart.Value;
                    staff.LunchBreakEnd = request.LunchBreakEnd.Value;
                }

                if (request.Certifications != null)
                    staff.Certifications = request.Certifications;

                if (request.Languages != null)
                    staff.Languages = request.Languages;

                if (request.InstagramHandle != null)
                    staff.InstagramHandle = request.InstagramHandle;

                await _context.SaveChangesAsync();

                var updatedProfile = await GetStaffProfileAsync(staff.Id);

                _logger.LogInformation($"Staff profile updated: {staff.Email}");
                return Ok(ApiResponse<StaffProfileResponseDto>.SuccessResponse(updatedProfile, "Profile updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff profile");
                return StatusCode(500, ApiResponse<StaffProfileResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpPost("specializations")]
        [Authorize(Roles = "Staff")]
        public async Task<ActionResult<ApiResponse<StaffSpecializationResponseDto>>> AddSpecialization(CreateStaffSpecializationDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<StaffSpecializationResponseDto>.ErrorResponse("Invalid token"));
                }

                var staff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId.Value);

                if (staff == null)
                {
                    return NotFound(ApiResponse<StaffSpecializationResponseDto>.ErrorResponse("Staff profile not found"));
                }

                // Check if service exists and belongs to the same salon
                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.SalonId == staff.SalonId);

                if (service == null)
                {
                    return BadRequest(ApiResponse<StaffSpecializationResponseDto>.ErrorResponse("Service not found or not available in your salon"));
                }

                // Check if specialization already exists
                var existingSpec = await _context.StaffSpecializations
                    .FirstOrDefaultAsync(s => s.StaffId == staff.Id && s.ServiceId == request.ServiceId);

                if (existingSpec != null)
                {
                    return BadRequest(ApiResponse<StaffSpecializationResponseDto>.ErrorResponse("You already have this specialization"));
                }

                var specialization = new StaffSpecialization
                {
                    StaffId = staff.Id,
                    ServiceId = request.ServiceId,
                    ProficiencyLevel = request.ProficiencyLevel,
                    ServiceTimeMultiplier = request.ServiceTimeMultiplier,
                    IsPreferred = request.IsPreferred,
                    Notes = request.Notes
                };

                _context.StaffSpecializations.Add(specialization);
                await _context.SaveChangesAsync();

                var response = new StaffSpecializationResponseDto
                {
                    Id = specialization.Id,
                    ServiceId = service.Id,
                    ServiceName = service.Name,
                    ProficiencyLevel = specialization.ProficiencyLevel,
                    ServiceTimeMultiplier = specialization.ServiceTimeMultiplier,
                    IsPreferred = specialization.IsPreferred,
                    Notes = specialization.Notes
                };

                _logger.LogInformation($"Specialization added: {staff.Email} -> {service.Name}");
                return Ok(ApiResponse<StaffSpecializationResponseDto>.SuccessResponse(response, "Specialization added successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding specialization");
                return StatusCode(500, ApiResponse<StaffSpecializationResponseDto>.ErrorResponse("An error occurred"));
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
        public async Task<ActionResult<ApiResponse<StaffProfileResponseDto>>> GetStaff(int id)
        {
            try
            {
                var staff = await GetStaffProfileAsync(id);
                if (staff == null)
                {
                    return NotFound(ApiResponse<StaffProfileResponseDto>.ErrorResponse("Staff member not found"));
                }

                return Ok(ApiResponse<StaffProfileResponseDto>.SuccessResponse(staff));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff member");
                return StatusCode(500, ApiResponse<StaffProfileResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        // Helper methods
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }

        private async Task<StaffProfileResponseDto?> GetStaffProfileAsync(int staffId)
        {
            var staff = await _context.Staff
                .Include(s => s.Salon)
                .Include(s => s.User)
                .Include(s => s.Specializations)
                    .ThenInclude(sp => sp.Service)
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (staff == null)
                return null;

            return new StaffProfileResponseDto
            {
                Id = staff.Id,
                UserId = staff.UserId ?? 0,
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
                EfficiencyMultiplier = staff.EfficiencyMultiplier,
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
                UpdatedAt = staff.UpdatedAt,
                Specializations = staff.Specializations.Select(sp => new StaffSpecializationResponseDto
                {
                    Id = sp.Id,
                    ServiceId = sp.ServiceId,
                    ServiceName = sp.Service.Name,
                    ProficiencyLevel = sp.ProficiencyLevel,
                    ServiceTimeMultiplier = sp.ServiceTimeMultiplier,
                    IsPreferred = sp.IsPreferred,
                    Notes = sp.Notes
                }).ToList()
            };
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

        // Mock data methods (will be replaced with real data in later steps)
        private async Task<StaffProfileSummaryDto> GetStaffProfileSummaryAsync(int staffId)
        {
            var staff = await _context.Staff
                .Include(s => s.Salon)
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (staff == null)
                throw new ArgumentException("Staff not found");

            return new StaffProfileSummaryDto
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Role = staff.Role,
                PhotoUrl = staff.PhotoUrl,
                ExperienceYears = staff.ExperienceYears,
                AverageRating = staff.AverageRating,
                TotalReviews = staff.TotalReviews,
                SalonName = staff.Salon.Name,
                IsOnShift = false, // Will be implemented in Step 4C
                ShiftStart = staff.StartTime,
                ShiftEnd = staff.EndTime,
                CurrentStatus = "OffShift" // Will be dynamic in Step 4C
            };
        }

        private async Task<TodayWorkSummaryDto> GetTodayWorkSummaryAsync(int staffId)
        {
            var staffWorkService = HttpContext.RequestServices.GetService<IStaffWorkService>();
            if (staffWorkService != null)
            {
                var workStatus = await staffWorkService.GetCurrentWorkStatusAsync(staffId);

                return new TodayWorkSummaryDto
                {
                    WorkDate = workStatus.WorkDate,
                    ClockInTime = workStatus.ClockInTime,
                    ClockOutTime = workStatus.ClockOutTime,
                    IsClockedIn = workStatus.IsClockedIn,
                    ScheduledMinutes = workStatus.ScheduledMinutes,
                    WorkedMinutes = workStatus.WorkedMinutes,
                    ServicesCompleted = 0, // Will be calculated from actual data
                    ServicesRemaining = await GetRemainingServicesCountAsync(staffId),
                    TodayEarnings = 0, // Will be calculated from actual data
                    TodayCommission = 0, // Will be calculated from actual data
                    UtilizationPercentage = workStatus.UtilizationPercentage,
                    NextBreakTime = workStatus.NextBreakTime,
                    CurrentStatus = workStatus.CurrentStatus,
                    StatusMessage = workStatus.StatusMessage
                };
            }

            // Fallback to mock data
            return new TodayWorkSummaryDto
            {
                WorkDate = DateTime.Today,
                ClockInTime = null,
                ClockOutTime = null,
                IsClockedIn = false,
                ScheduledMinutes = 480,
                WorkedMinutes = 0,
                ServicesCompleted = 0,
                ServicesRemaining = 3,
                TodayEarnings = 0,
                TodayCommission = 0,
                UtilizationPercentage = 0,
                NextBreakTime = TimeSpan.FromHours(13),
                CurrentStatus = "OffShift",
                StatusMessage = "You haven't clocked in yet today"
            };
        }

        private async Task<int> GetRemainingServicesCountAsync(int staffId)
        {
            var today = DateTime.Today;
            var now = DateTime.Now.TimeOfDay;

            return await _context.Bookings
                .CountAsync(b => b.AssignedStaffId == staffId &&
                                b.BookingDate.Date == today &&
                                b.BookingTime > now &&
                                b.Status != "Cancelled");
        }
        private async Task<List<TodayBookingDto>> GetTodayBookingsAsync(int staffId)
        {
            var today = DateTime.Today;

            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Service)
                .Where(b => b.AssignedStaffId == staffId &&
                           b.BookingDate.Date == today &&
                           b.Status != "Cancelled")
                .Select(b => new TodayBookingDto
                {
                    BookingId = b.Id,
                    CustomerName = $"{b.Customer.FirstName} {b.Customer.LastName}",
                    CustomerPhone = b.Customer.PhoneNumber,
                    CustomerPhoto = "", // Will implement photo URLs later
                    ServiceName = b.Service.Name,
                    ScheduledTime = b.BookingTime,
                    // Fix the TimeSpan conversion issues
                    EstimatedStartTime = b.EstimatedStartTime ?? b.BookingTime,
                    EstimatedEndTime = b.EstimatedEndTime ?? b.BookingTime.Add(TimeSpan.FromMinutes(b.Service.DurationInMinutes)),
                    DurationMinutes = b.EstimatedDurationMinutes > 0 ? b.EstimatedDurationMinutes : b.Service.DurationInMinutes,
                    ServicePrice = b.TotalAmount,
                    ExpectedCommission = b.TotalAmount * 0.4m, // Will use actual staff commission rate later
                    Status = b.Status,
                    ServiceStatus = "Scheduled", // Will be dynamic in Step 4D
                    Notes = b.Notes,
                    IsRunningLate = false, // Will calculate in Step 4D
                    DelayMinutes = 0,
                    IsNextBooking = false, // Will calculate based on current time
                    CanStartEarly = false, // Will implement logic later
                    CustomerPreferences = "" // Will load from customer preferences
                })
                .OrderBy(b => b.ScheduledTime)
                .ToListAsync();
        }

        private async Task<StaffPerformanceDto> GetStaffPerformanceAsync(int staffId)
        {
            // Mock data - will be implemented in Step 4E
            return new StaffPerformanceDto
            {
                TodayServices = 0,
                TodayEarnings = 0,
                WeekServices = 12,
                WeekEarnings = 4800,
                MonthServices = 45,
                MonthEarnings = 18000,
                AverageRating = 4.8m,
                TotalReviews = 127,
                UtilizationPercentage = 85,
                TopServices = new List<string> { "Hair Coloring", "Hair Cut", "Highlights" },
                Achievements = new List<string> { "⭐ Customer Favorite", "🎯 Top Performer" }
            };
        }

        private async Task<List<StaffNotificationDto>> GetStaffNotificationsAsync(int staffId)
        {
            // Mock data - will be implemented in Step 4G
            return new List<StaffNotificationDto>
    {
        new StaffNotificationDto
        {
            Id = 1,
            Type = "Reminder",
            Title = "Lunch Break Soon",
            Message = "Your lunch break starts in 30 minutes",
            CreatedAt = DateTime.Now.AddMinutes(-10),
            IsRead = false,
            Priority = "Low",
            ActionUrl = "",
            ActionText = ""
        },
        new StaffNotificationDto
        {
            Id = 2,
            Type = "Booking",
            Title = "New Booking Added",
            Message = "Sarah Johnson booked Hair Coloring for 3:00 PM",
            CreatedAt = DateTime.Now.AddHours(-2),
            IsRead = false,
            Priority = "Medium",
            ActionUrl = "/bookings/123",
            ActionText = "View Booking"
        }
    };
        }

        private async Task<StaffTargetDto> GetMonthlyTargetAsync(int staffId)
        {
            // Mock data - will be implemented in Step 4F
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            return new StaffTargetDto
            {
                Month = currentMonth,
                Year = currentYear,
                MonthName = new DateTime(currentYear, currentMonth, 1).ToString("MMMM"),
                TargetRevenue = 25000,
                ActualRevenue = 18000,
                RevenueProgress = 72,
                TargetServices = 80,
                ActualServices = 45,
                ServiceProgress = 56.25m,
                TargetRating = 4.5m,
                ActualRating = 4.8m,
                PotentialBonus = 2500,
                BonusEligible = false,
                BonusStatus = "In Progress",
                Achievements = new List<string> { "⭐ Excellence in Service Quality" },
                Recommendations = new List<string> { "Focus on booking more appointments to reach service target" }
            };
        }
    }
}
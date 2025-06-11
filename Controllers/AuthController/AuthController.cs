using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Auth;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Services.Interfaces;
using System.Security.Claims;

namespace stibe.api.Controllers.AuthController
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
        }
        [HttpPost("register-admin")]
        [Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can create other admins
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> RegisterAdmin(AdminRegistrationDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<AdminUserDto>.ErrorResponse("Invalid token"));
                }

                // Check if user already exists
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

                if (existingUserByEmail != null)
                {
                    return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse("Email already exists"));
                }

                if (existingUserByPhone != null)
                {
                    return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse("Phone number already exists"));
                }

                // Create new admin user
                var adminUser = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = _passwordService.HashPassword(request.Password),
                    Role = "Admin",
                    IsEmailVerified = false,

                    // Admin-specific properties
                    IsSystemAdmin = request.IsSystemAdmin,
                    CanMonitorSalons = request.CanMonitorSalons,
                    CanMonitorStaff = request.CanMonitorStaff,
                    CanMonitorBookings = request.CanMonitorBookings,
                    CanMonitorUsers = request.CanMonitorUsers,
                    CanModifySystemSettings = request.CanModifySystemSettings,
                    AdminRoleAssignedDate = DateTime.UtcNow,
                    AdminRoleAssignedBy = currentUserId
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                // Send verification email
                var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?email={adminUser.Email}&token=mock-token";
                await _emailService.SendVerificationEmailAsync(adminUser.Email, verificationLink);

                var adminDto = new AdminUserDto
                {
                    Id = adminUser.Id,
                    FirstName = adminUser.FirstName,
                    LastName = adminUser.LastName,
                    Email = adminUser.Email,
                    PhoneNumber = adminUser.PhoneNumber,
                    Role = adminUser.Role,
                    IsEmailVerified = adminUser.IsEmailVerified,
                    CreatedAt = adminUser.CreatedAt,

                    // Admin-specific properties
                    IsSystemAdmin = adminUser.IsSystemAdmin,
                    CanMonitorSalons = adminUser.CanMonitorSalons,
                    CanMonitorStaff = adminUser.CanMonitorStaff,
                    CanMonitorBookings = adminUser.CanMonitorBookings,
                    CanMonitorUsers = adminUser.CanMonitorUsers,
                    CanModifySystemSettings = adminUser.CanModifySystemSettings,
                    AdminRoleAssignedDate = adminUser.AdminRoleAssignedDate
                };

                _logger.LogInformation($"Admin user registered successfully: {adminUser.Email} by {currentUserId}");
                return Ok(ApiResponse<AdminUserDto>.SuccessResponse(adminDto, "Admin user registered successfully. Please check the email for verification."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin user registration");
                return StatusCode(500, ApiResponse<AdminUserDto>.ErrorResponse("An error occurred during admin registration"));
            }
        }

        // Add this endpoint to initialize the first SuperAdmin (only works when no admin exists)
        [HttpPost("initialize-super-admin")]
        [AllowAnonymous] // This should only be allowed once during system setup
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> InitializeSuperAdmin(AdminRegistrationDto request)
        {
            try
            {
                // Check if any admin already exists
                var existingAdmin = await _context.Users
                    .AnyAsync(u => u.Role == "Admin" || u.Role == "SuperAdmin");

                if (existingAdmin)
                {
                    return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse("Super Admin already exists. Cannot initialize again."));
                }

                // Check if user already exists
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUserByEmail != null)
                {
                    return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse("Email already exists"));
                }

                // Create new super admin user
                var superAdmin = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = _passwordService.HashPassword(request.Password),
                    Role = "SuperAdmin",
                    IsEmailVerified = true, // Auto-verified for initial setup

                    // Admin-specific properties
                    IsSystemAdmin = true,
                    CanMonitorSalons = true,
                    CanMonitorStaff = true,
                    CanMonitorBookings = true,
                    CanMonitorUsers = true,
                    CanModifySystemSettings = true,
                    AdminRoleAssignedDate = DateTime.UtcNow
                };

                _context.Users.Add(superAdmin);
                await _context.SaveChangesAsync();

                var adminDto = new AdminUserDto
                {
                    Id = superAdmin.Id,
                    FirstName = superAdmin.FirstName,
                    LastName = superAdmin.LastName,
                    Email = superAdmin.Email,
                    PhoneNumber = superAdmin.PhoneNumber,
                    Role = superAdmin.Role,
                    IsEmailVerified = superAdmin.IsEmailVerified,
                    CreatedAt = superAdmin.CreatedAt,

                    // Admin-specific properties
                    IsSystemAdmin = superAdmin.IsSystemAdmin,
                    CanMonitorSalons = superAdmin.CanMonitorSalons,
                    CanMonitorStaff = superAdmin.CanMonitorStaff,
                    CanMonitorBookings = superAdmin.CanMonitorBookings,
                    CanMonitorUsers = superAdmin.CanMonitorUsers,
                    CanModifySystemSettings = superAdmin.CanModifySystemSettings,
                    AdminRoleAssignedDate = superAdmin.AdminRoleAssignedDate
                };

                _logger.LogInformation($"Super Admin initialized successfully: {superAdmin.Email}");
                return Ok(ApiResponse<AdminUserDto>.SuccessResponse(adminDto, "Super Admin initialized successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Super Admin");
                return StatusCode(500, ApiResponse<AdminUserDto>.ErrorResponse("An error occurred during Super Admin initialization"));
            }
        }
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<UserDto>>> Register(RegisterRequestDto request)
        {
            try
            {
                // Validate role
                if (request.Role != "Customer" && request.Role != "SalonOwner")
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Invalid role. Must be 'Customer' or 'SalonOwner'"));
                }

                // Check if user already exists
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

                if (existingUserByEmail != null)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Email already exists"));
                }

                if (existingUserByPhone != null)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Phone number already exists"));
                }

                // Create new user
                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = _passwordService.HashPassword(request.Password),
                    Role = request.Role,
                    IsEmailVerified = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Send verification email
                var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?email={user.Email}&token=mock-token";
                await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt
                };

                _logger.LogInformation($"User registered successfully: {user.Email}");
                return Ok(ApiResponse<UserDto>.SuccessResponse(userDto, "User registered successfully. Please check your email for verification."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("An error occurred during registration"));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginRequestDto request)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Email == request.Email && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid email or password"));
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

                _logger.LogInformation($"User logged in successfully: {user.Email}");
                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, "Login successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse("An error occurred during login"));
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt
                };

                return Ok(ApiResponse<UserDto>.SuccessResponse(userDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePassword(ChangePasswordRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(ApiResponse.ErrorResponse("User not found"));
                }

                if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(ApiResponse.ErrorResponse("Current password is incorrect"));
                }

                user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password changed successfully for user: {user.Email}");
                return Ok(ApiResponse.SuccessResponse("Password changed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while changing password"));
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse>> ForgotPassword(ForgotPasswordRequestDto request)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Email == request.Email && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Ok(ApiResponse.SuccessResponse("If the email exists, a password reset link has been sent"));
                }

                var resetToken = _passwordService.GenerateResetToken();
                var resetLink = $"{Request.Scheme}://{Request.Host}/api/auth/reset-password?email={user.Email}&token={resetToken}";

                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

                _logger.LogInformation($"Password reset requested for user: {user.Email}");
                return Ok(ApiResponse.SuccessResponse("If the email exists, a password reset link has been sent"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password request");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("verify-email")]
        public async Task<ActionResult<ApiResponse>> VerifyEmail(string email, string token)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Email == email && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BadRequest(ApiResponse.ErrorResponse("Invalid verification link"));
                }

                if (user.IsEmailVerified)
                {
                    return Ok(ApiResponse.SuccessResponse("Email already verified"));
                }

                user.IsEmailVerified = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Email verified successfully for user: {user.Email}");
                return Ok(ApiResponse.SuccessResponse("Email verified successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred"));
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}
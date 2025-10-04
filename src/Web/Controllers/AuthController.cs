using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using stibe.api.Configuration;
using stibe.api.Data;
using stibe.api.Models.DTOs.Auth;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Services.Interfaces;
using stibe.api.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace stibe.api.Controllers
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
        private readonly IWebHostEnvironment _environment;
        private readonly IGoogleOAuthService _googleOAuthService;
        private readonly IConfiguration _configuration;
        private readonly IUserCouponService _userCouponService;
        private readonly IFileService _fileService;

        public AuthController(
            ApplicationDbContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IWebHostEnvironment environment,
            IGoogleOAuthService googleOAuthService,
            IConfiguration configuration,
            IUserCouponService userCouponService,
            IFileService fileService)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
            _environment = environment;
            _googleOAuthService = googleOAuthService;
            _configuration = configuration;
            _userCouponService = userCouponService;
            _fileService = fileService;
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
                    CanMonitorShops = request.CanMonitorShops,
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
                var verificationToken = _passwordService.GenerateSecureToken();
                var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/direct-verify-email?email={Uri.EscapeDataString(adminUser.Email)}&token={verificationToken}";
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
                    CanMonitorShops = adminUser.CanMonitorShops,
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
                    CanMonitorShops = true,
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
                    CanMonitorShops = superAdmin.CanMonitorShops,
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

        #region Google OAuth Methods

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> GoogleLogin(GoogleLoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("Google login attempt");

                // Validate Google token and get user info
                var googleUserInfo = await _googleOAuthService.ValidateGoogleTokenAsync(request.GoogleToken);
                if (googleUserInfo == null)
                {
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid Google token"));
                }

                _logger.LogInformation($"Google login attempt for email: {googleUserInfo.Email}");

                // Check if user already exists
                var existingUser = await _context.Users
                    .Where(u => u.Email.ToLower() == googleUserInfo.Email.ToLower() && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                User user;

                if (existingUser != null)
                {
                    // Update existing user with Google info if not already set
                    user = existingUser;
                    
                    // Update Google ID if not set
                    try
                    {
                        var googleIdProp = typeof(User).GetProperty("GoogleId");
                        if (googleIdProp != null)
                        {
                            var currentGoogleId = googleIdProp.GetValue(user)?.ToString();
                            if (string.IsNullOrEmpty(currentGoogleId))
                            {
                                googleIdProp.SetValue(user, googleUserInfo.GoogleId);
                            }
                        }

                        // Update profile picture if not set
                        if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(googleUserInfo.Picture))
                        {
                            user.ProfilePictureUrl = googleUserInfo.Picture;
                        }

                        // Ensure email is verified for Google users
                        if (!user.IsEmailVerified && googleUserInfo.EmailVerified)
                        {
                            user.IsEmailVerified = true;
                        }

                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not update Google properties for existing user");
                        // Continue with login even if update fails
                    }
                }
                else
                {
                    // Create new user from Google info
                    user = new User
                    {
                        FirstName = googleUserInfo.FirstName,
                        LastName = googleUserInfo.LastName,
                        Email = googleUserInfo.Email.ToLower().Trim(),
                        PhoneNumber = "", // Will be updated later by user if needed
                        PasswordHash = _passwordService.HashPassword(Guid.NewGuid().ToString()), // Random password for Google users
                        Role = request.Role,
                        IsEmailVerified = googleUserInfo.EmailVerified,
                        ProfilePictureUrl = googleUserInfo.Picture
                    };

                    // Set Google ID if property exists
                    try
                    {
                        var googleIdProp = typeof(User).GetProperty("GoogleId");
                        if (googleIdProp != null)
                        {
                            googleIdProp.SetValue(user, googleUserInfo.GoogleId);
                        }

                        // Set registration IP if property exists
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        if (!string.IsNullOrEmpty(ipAddress))
                        {
                            var prop = typeof(User).GetProperty("RegistrationIP");
                            if (prop != null)
                            {
                                prop.SetValue(user, ipAddress);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not set Google properties for new user");
                        // Continue with user creation
                    }

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"New user created via Google OAuth: {user.Email}");
                }

                // Update last login info
                try
                {
                    var lastLoginDateProp = typeof(User).GetProperty("LastLoginDate");
                    if (lastLoginDateProp != null)
                    {
                        lastLoginDateProp.SetValue(user, DateTime.UtcNow);
                    }

                    var lastLoginIPProp = typeof(User).GetProperty("LastLoginIP");
                    if (lastLoginIPProp != null)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        lastLoginIPProp.SetValue(user, ipAddress);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not update last login info");
                    // Continue with login even if update fails
                }

                // Generate JWT tokens
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
                        CreatedAt = user.CreatedAt,
                        ProfilePictureUrl = user.ProfilePictureUrl
                    }
                };

                _logger.LogInformation($"Google login successful for: {user.Email}");
                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, $"Welcome back, {user.FirstName}!"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse("An error occurred during Google login. Please try again later."));
            }
        }

        [HttpPost("google-register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<UserDto>>> GoogleRegister(GoogleLoginRequestDto request)
        {
            try
            {
                _logger.LogInformation("Google registration attempt");

                // Validate Google token and get user info
                var googleUserInfo = await _googleOAuthService.ValidateGoogleTokenAsync(request.GoogleToken);
                if (googleUserInfo == null)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Invalid Google token"));
                }

                _logger.LogInformation($"Google registration attempt for email: {googleUserInfo.Email}");

                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == googleUserInfo.Email.ToLower());

                if (existingUser != null)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("User with this email already exists. Please try logging in instead."));
                }

                // Validate role
                if (request.Role != "Customer" && request.Role != "ShopOwner")
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Invalid role. Must be 'Customer' or 'ShopOwner'"));
                }

                // Create new user from Google info
                var user = new User
                {
                    FirstName = googleUserInfo.FirstName,
                    LastName = googleUserInfo.LastName,
                    Email = googleUserInfo.Email.ToLower().Trim(),
                    PhoneNumber = "", // Will be updated later by user if needed
                    PasswordHash = _passwordService.HashPassword(Guid.NewGuid().ToString()), // Random password for Google users
                    Role = request.Role,
                    IsEmailVerified = googleUserInfo.EmailVerified,
                    ProfilePictureUrl = googleUserInfo.Picture
                };

                // Set Google ID and other optional properties
                try
                {
                    var googleIdProp = typeof(User).GetProperty("GoogleId");
                    if (googleIdProp != null)
                    {
                        googleIdProp.SetValue(user, googleUserInfo.GoogleId);
                    }

                    // Set registration IP if property exists
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    if (!string.IsNullOrEmpty(ipAddress))
                    {
                        var prop = typeof(User).GetProperty("RegistrationIP");
                        if (prop != null)
                        {
                            prop.SetValue(user, ipAddress);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not set optional properties for Google user");
                    // Continue with user creation
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    ProfilePictureUrl = user.ProfilePictureUrl
                };

                _logger.LogInformation($"Google user registered successfully: {user.Email}");
                return Ok(ApiResponse<UserDto>.SuccessResponse(userDto, "Registration successful via Google! You can now log in."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google registration");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("An error occurred during Google registration. Please try again later."));
            }
        }

        #endregion

        [HttpGet("debug-google-auth")]
        [AllowAnonymous]
        public ActionResult<object> DebugGoogleAuth()
        {
            try
            {
                var settings = new
                {
                    GoogleOAuthEnabled = _googleOAuthService != null,
                    ClientId = _googleOAuthService != null ? "Configured" : "Not Configured",
                    SupportedAudiences = new[]
                    {
                        "986486622148-0811nmnfmnjmnc0er554rvlqpn6dlvpl.apps.googleusercontent.com (Android/Web)"
                    },
                    Endpoints = new[]
                    {
                        "POST /api/auth/google-login",
                        "POST /api/auth/google-register"
                    }
                };

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug Google auth");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("validate-google-token")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> ValidateGoogleToken([FromBody] object tokenRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(tokenRequest);
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                if (tokenData == null || !tokenData.ContainsKey("token"))
                {
                    return BadRequest(new { error = "Token is required", field = "token" });
                }

                var token = tokenData["token"]?.ToString();
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { error = "Token cannot be empty" });
                }

                var userInfo = await _googleOAuthService.ValidateGoogleTokenAsync(token);
                
                if (userInfo == null)
                {
                    return BadRequest(new { 
                        error = "Invalid Google token",
                        details = "Token validation failed. Please ensure you're using a valid Google ID token."
                    });
                }

                return Ok(new
                {
                    valid = true,
                    userInfo = userInfo,
                    message = "Token is valid and ready for authentication"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google token");
                return StatusCode(500, new { 
                    error = "Internal server error",
                    details = ex.Message
                });
            }
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserDto>>> Register(RegisterRequestDto request)
        {
            try
            {
                var validationErrors = new List<string>();

                // Enhanced validation
                if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length < 1)
                {
                    validationErrors.Add("First name is required");
                }

                // Make LastName optional but if provided, must be at least 1 character
                if (!string.IsNullOrWhiteSpace(request.LastName) && request.LastName.Length < 1)
                {
                    validationErrors.Add("Last name must be at least 1 character when provided");
                }

                // Enhanced email validation beyond the basic attribute
                if (!IsValidEmail(request.Email))
                {
                    validationErrors.Add("Please provide a valid email address");
                }

                // Enhanced phone validation beyond the basic attribute
                if (!IsValidPhoneNumber(request.PhoneNumber))
                {
                    validationErrors.Add("Please provide a valid phone number");
                }

                // Enhanced password validation
                var passwordValidation = ValidatePassword(request.Password);
                if (passwordValidation.Count > 0)
                {
                    validationErrors.AddRange(passwordValidation);
                }

                // Validate role
                if (request.Role != "Customer" && request.Role != "ShopOwner")
                {
                    validationErrors.Add("Invalid role. Must be 'Customer' or 'ShopOwner'");
                }

                if (validationErrors.Any())
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse(validationErrors));
                }

                // Check if user already exists
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                var existingUserByPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

                if (existingUserByEmail != null)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Email address is already registered. Please use a different email or try logging in."));
                }

                if (existingUserByPhone != null)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Phone number is already registered. Please use a different phone number."));
                }

                // Sanitize inputs
                var firstName = SanitizeInput(request.FirstName);
                var lastName = string.IsNullOrWhiteSpace(request.LastName) ? "" : SanitizeInput(request.LastName);
                var email = request.Email.ToLower().Trim();

                // Create new user
                var user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PhoneNumber = request.PhoneNumber.Trim(),
                    PasswordHash = _passwordService.HashPassword(request.Password),
                    Role = request.Role,
                    IsEmailVerified = false
                };

                // Add registration IP if the property exists
                try
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    if (ipAddress != null)
                    {
                        var prop = typeof(User).GetProperty("RegistrationIP");
                        if (prop != null)
                        {
                            prop.SetValue(user, ipAddress);
                        }
                    }
                }
                catch
                {
                    // Ignore if property doesn't exist
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate a secure verification token
                var verificationToken = _passwordService.GenerateSecureToken();

                // Direct verification link that verifies in the API itself
                var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/direct-verify-email?email={Uri.EscapeDataString(user.Email)}&token={verificationToken}";
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
                return Ok(ApiResponse<UserDto>.SuccessResponse(userDto,
                    "Registration successful! Please check your email to verify your account before logging in."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("An error occurred during registration. Please try again later."));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginRequestDto request)
        {
            try
            {
                _logger.LogInformation($"Login attempt for email: {request.Email}");
                
                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning($"User not found for email: {request.Email}");
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid email or password"));
                }

                if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Invalid password for user: {request.Email}");
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid email or password"));
                }                // Check if email is verified
                if (!user.IsEmailVerified)
                {
                    // Generate and send a new verification email
                    var verificationToken = _passwordService.GenerateSecureToken();
                    var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/direct-verify-email?email={Uri.EscapeDataString(user.Email)}&token={verificationToken}";
                    await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse(
                        "Your email address is not verified. A new verification link has been sent to your email. Please check your inbox and click the verification link before logging in."));
                }

                var token = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(60);

                // Update last login date if the property exists
                try
                {
                    var lastLoginDateProp = typeof(User).GetProperty("LastLoginDate");
                    if (lastLoginDateProp != null)
                    {
                        lastLoginDateProp.SetValue(user, DateTime.UtcNow);
                    }

                    var lastLoginIPProp = typeof(User).GetProperty("LastLoginIP");
                    if (lastLoginIPProp != null)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        lastLoginIPProp.SetValue(user, ipAddress);
                    }

                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Ignore if properties don't exist
                }

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
                        CreatedAt = user.CreatedAt,
                        ProfilePictureUrl = user.ProfilePictureUrl
                    }
                };

                _logger.LogInformation($"User logged in successfully: {user.Email}");
                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, $"Welcome back, {user.FirstName}!"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse("An error occurred during login. Please try again later."));
            }
        }

        [HttpGet("direct-reset-password")]
        [AllowAnonymous]
        public IActionResult DirectResetPassword(string email, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                {
                    return BadRequest("Invalid reset link");
                }

                // Return a password reset HTML form
                return Content($@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Reset Your Password - Stibe Booking</title>
            <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; background-color: #f5f5f5; }}
                .container {{ max-width: 500px; margin: 40px auto; padding: 20px; background-color: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                h1 {{ color: #4A86E8; margin-top: 0; text-align: center; }}
                .form-group {{ margin-bottom: 20px; }}
                label {{ display: block; margin-bottom: 5px; font-weight: bold; }}
                input[type=""password""] {{ width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; font-size: 16px; box-sizing: border-box; }}
                .submit-btn {{ background-color: #4CAF50; color: white; border: none; padding: 12px 20px; width: 100%; font-size: 16px; font-weight: bold; 
                              border-radius: 4px; cursor: pointer; }}
                .submit-btn:hover {{ background-color: #45a049; }}
                .error {{ color: #ff0000; margin-top: 5px; display: none; }}
                .passwordRequirements {{ font-size: 13px; color: #666; margin-top: 10px; }}
                .requirement {{ margin: 5px 0; }}
                .valid {{ color: #4CAF50; }}
                .invalid {{ color: #ff0000; }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <h1>Reset Your Password</h1>
                <form id=""resetForm"" method=""POST"" action=""/api/auth/complete-reset-password"" enctype=""application/x-www-form-urlencoded"">
                    <input type=""hidden"" name=""email"" value=""{email}"">
                    <input type=""hidden"" name=""token"" value=""{token}"">
                    
                    <div class=""form-group"">
                        <label for=""newPassword"">New Password</label>
                        <input type=""password"" id=""newPassword"" name=""newPassword"" required>
                        <div id=""passwordError"" class=""error"">Password doesn't meet requirements</div>
                        <div class=""passwordRequirements"">
                            <p>Password must contain:</p>
                            <div class=""requirement"" id=""length"">• At least 8 characters</div>
                            <div class=""requirement"" id=""uppercase"">• At least 1 uppercase letter</div>
                            <div class=""requirement"" id=""lowercase"">• At least 1 lowercase letter</div>
                            <div class=""requirement"" id=""number"">• At least 1 number</div>
                            <div class=""requirement"" id=""special"">• At least 1 special character</div>
                        </div>
                    </div>
                    
                    <div class=""form-group"">
                        <label for=""confirmPassword"">Confirm Password</label>
                        <input type=""password"" id=""confirmPassword"" name=""confirmPassword"" required>
                        <div id=""matchError"" class=""error"">Passwords don't match</div>
                    </div>
                    
                    <button type=""submit"" class=""submit-btn"">Reset Password</button>
                </form>
            </div>
            
            <script>
                const form = document.getElementById('resetForm');
                const newPassword = document.getElementById('newPassword');
                const confirmPassword = document.getElementById('confirmPassword');
                const passwordError = document.getElementById('passwordError');
                const matchError = document.getElementById('matchError');
                
                // Password validation elements
                const length = document.getElementById('length');
                const uppercase = document.getElementById('uppercase');
                const lowercase = document.getElementById('lowercase');
                const number = document.getElementById('number');
                const special = document.getElementById('special');
                
                function validatePassword() {{
                    const password = newPassword.value;
                    let valid = true;
                    
                    // Length check
                    if (password.length >= 8) {{
                        length.className = ""requirement valid"";
                        length.textContent = ""✓ At least 8 characters"";
                    }} else {{
                        length.className = ""requirement invalid"";
                        length.textContent = ""• At least 8 characters"";
                        valid = false;
                    }}
                    
                    // Uppercase check
                    if (/[A-Z]/.test(password)) {{
                        uppercase.className = ""requirement valid"";
                        uppercase.textContent = ""✓ At least 1 uppercase letter"";
                    }} else {{
                        uppercase.className = ""requirement invalid"";
                        uppercase.textContent = ""• At least 1 uppercase letter"";
                        valid = false;
                    }}
                    
                    // Lowercase check
                    if (/[a-z]/.test(password)) {{
                        lowercase.className = ""requirement valid"";
                        lowercase.textContent = ""✓ At least 1 lowercase letter"";
                    }} else {{
                        lowercase.className = ""requirement invalid"";
                        lowercase.textContent = ""• At least 1 lowercase letter"";
                        valid = false;
                    }}
                    
                    // Number check
                    if (/[0-9]/.test(password)) {{
                        number.className = ""requirement valid"";
                        number.textContent = ""✓ At least 1 number"";
                    }} else {{
                        number.className = ""requirement invalid"";
                        number.textContent = ""• At least 1 number"";
                        valid = false;
                    }}
                    
                    // Special character check
                    if (/[^A-Za-z0-9]/.test(password)) {{
                        special.className = ""requirement valid"";
                        special.textContent = ""✓ At least 1 special character"";
                    }} else {{
                        special.className = ""requirement invalid"";
                        special.textContent = ""• At least 1 special character"";
                        valid = false;
                    }}
                    
                    return valid;
                }}
                
                function checkPasswordMatch() {{
                    if (newPassword.value !== confirmPassword.value) {{
                        matchError.style.display = 'block';
                        return false;
                    }} else {{
                        matchError.style.display = 'none';
                        return true;
                    }}
                }}
                
                newPassword.addEventListener('input', function() {{
                    validatePassword();
                    if (confirmPassword.value) {{
                        checkPasswordMatch();
                    }}
                }});
                
                confirmPassword.addEventListener('input', checkPasswordMatch);
                
                form.addEventListener('submit', function(e) {{
                    const isPasswordValid = validatePassword();
                    const isMatchValid = checkPasswordMatch();
                    
                    if (!isPasswordValid) {{
                        e.preventDefault();
                        passwordError.style.display = 'block';
                    }} else {{
                        passwordError.style.display = 'none';
                    }}
                    
                    if (!isMatchValid) {{
                        e.preventDefault();
                    }}
                }});
            </script>
        </body>
        </html>
        ", "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset form");
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        [HttpPost("complete-reset-password")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> CompleteResetPassword([FromForm] CompleteResetPasswordDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest("Invalid reset request");
                }

                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    // For security, don't disclose if the email exists
                    return Content($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Password Reset Error</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }}
                        .error {{ color: #e74c3c; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1 class='error'>Invalid Reset Link</h1>
                        <p>The password reset link is invalid or has expired.</p>
                        <p>Please request a new password reset link from the login page.</p>
                        <a href='{_configuration["App:LoginRedirectUrl"] ?? "/login"}'>Go to Login</a>
                    </div>
                </body>
                </html>
            ", "text/html");
                }

                // In a real application, validate the token here
                // For the implementation, we'll just accept the token for now

                // Validate new password
                var passwordValidation = ValidatePassword(request.NewPassword);
                if (passwordValidation.Count > 0)
                {
                    var errorList = string.Join("</li><li>", passwordValidation);
                    return Content($@"
                <!DOCTYPE html> 
                <html>
                <head>
                    <title>Password Reset Error</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }}
                        .error {{ color: #e74c3c; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                        ul {{ text-align: left; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1 class='error'>Password Reset Failed</h1>
                        <p>Your password does not meet the requirements:</p>
                        <ul><li>{errorList}</li></ul>
                        <p>Please go back and try again.</p>
                        <button onclick='history.back()'>Go Back</button>
                    </div>
                </body>
                </html>
            ", "text/html");
                }

                user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password reset successfully for user: {user.Email}");

                var loginRedirect = _configuration["App:LoginRedirectUrl"] ?? "/login";

                return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
                <title>Password Reset Success</title>
                <style>
                    :root {{ --bg:#0f1724; --card:#0b1220; --accent:#6ee7b7; --muted:#9ca3af; --glass: rgba(255,255,255,0.04); }}
                    html,body{{height:100%;margin:0;padding:0;font-family:Inter, Roboto, system-ui, -apple-system, 'Segoe UI', Arial; background: linear-gradient(180deg,#071021 0%, #071a2b 100%); color:#e6eef6}}
                    .wrap{{min-height:100%;display:flex;align-items:center;justify-content:center;padding:24px}}
                    .card{{width:100%;max-width:520px;background:linear-gradient(180deg, rgba(255,255,255,0.02), rgba(255,255,255,0.01));border-radius:16px;padding:28px;box-shadow:0 8px 30px rgba(2,6,23,0.6);border:1px solid rgba(255,255,255,0.03)}}
                    h1{{margin:0 0 12px;font-weight:600;font-size:22px;color:var(--accent)}}
                    p{{margin:0 0 12px;color:var(--muted)}}
                    .count{{font-weight:700;color:#fff;font-size:20px}}
                    .manual{{display:inline-block;margin-top:14px;padding:10px 16px;border-radius:10px;background:linear-gradient(90deg,#0ea5a9,#7c3aed);color:white;text-decoration:none}}
                </style>
            </head>
            <body>
                <div class='wrap'>
                    <div class='card'>
                        <h1>Password Reset Successfully</h1>
                        <p>Your password has been updated. You'll be redirected to the app to sign in automatically.</p>
                        <p id='message'>Redirecting to login in <span id='countdown' class='count'>3</span> seconds...</p>
                        <p><a id='manualLink' class='manual' href='{loginRedirect}'>Open app now</a></p>
                    </div>
                </div>
                <script>
                    (function(){{
                        var countdownEl = document.getElementById('countdown');
                        var seconds = 3;
                        var redirectUrl = '{loginRedirect}';

                        var interval = setInterval(function(){{
                            seconds--;
                            if (countdownEl) countdownEl.textContent = seconds;
                            if (seconds <= 0){{
                                clearInterval(interval);
                                try{{
                                    window.location.href = redirectUrl;
                                }}catch(e){{
                                    window.location.href = '/login';
                                }}
                            }}
                        }}, 1000);
                    }})();
                </script>
            </body>
            </html>
        ", "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, "An error occurred while resetting your password. Please try again later.");
            }
        }

        [HttpGet("check-verification")]
        public async Task<ActionResult<ApiResponse<object>>> CheckEmailVerification(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Email is required"));
                }

                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("User not found"));
                }

                var data = new { isEmailVerified = user.IsEmailVerified };
                return Ok(ApiResponse<object>.SuccessResponse(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email verification status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred"));
            }
        }        [HttpPost("resend-verification")]
        public async Task<ActionResult<ApiResponse<object>>> ResendVerificationEmail(ResendVerificationRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Email is required"));
                }

                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("User not found"));
                }

                if (user.IsEmailVerified)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Email is already verified"));
                }

                // Generate a new verification token
                var verificationToken = _passwordService.GenerateSecureToken();
                var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/direct-verify-email?email={Uri.EscapeDataString(user.Email)}&token={verificationToken}";
                
                await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

                _logger.LogInformation($"Verification email resent to: {user.Email}");
                return Ok(ApiResponse<object>.SuccessResponse(new { }, "Verification email sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while sending verification email"));
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> RefreshToken(RefreshTokenRequestDto request)
        {
            try
            {
                _logger.LogInformation("Token refresh attempt initiated");

                // In a production app, validate the refresh token against stored tokens
                var userId = _jwtService.GetUserIdFromToken(request.Token);
                if (userId == null)
                {
                    _logger.LogWarning("Token refresh failed: Invalid token format");
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning($"Token refresh failed: User not found for ID {userId}");
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid token"));
                }

                // Generate new tokens
                var newToken = _jwtService.GenerateToken(user);
                var newRefreshToken = _jwtService.GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(60);

                var response = new LoginResponseDto
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken,
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

                _logger.LogInformation($"Token refreshed successfully for user: {user.Email} (ID: {user.Id})");
                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, "Token refreshed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse("An error occurred while refreshing token"));
            }
        }

        [HttpGet("token-status")]
        [Authorize]
        public ActionResult<ApiResponse<object>> GetTokenStatus()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token"));
                }

                // If we get here, the token is valid
                var tokenInfo = new
                {
                    IsValid = true,
                    UserId = currentUserId,
                    CheckedAt = DateTime.UtcNow,
                    Message = "Token is valid and active"
                };

                _logger.LogInformation($"Token status checked for user ID: {currentUserId}");
                return Ok(ApiResponse<object>.SuccessResponse(tokenInfo, "Token status retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token status: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while checking token status"));
            }
        }

        [HttpGet("direct-verify-email")]
        public async Task<ActionResult> DirectVerifyEmail(string email, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                {
                    return BadRequest("Invalid verification link");
                }

                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BadRequest("Invalid verification link");
                }

                if (user.IsEmailVerified)
                {
                    // Already verified - redirect to app login with a short countdown
                    var loginRedirectUrl1 = _configuration["App:LoginRedirectUrl"] ?? "/login";

                    // Attempt to generate short-lived tokens for auto-login if possible
                    string shortToken = string.Empty;
                    string shortRefreshToken = string.Empty;
                    string shortExpiresAt = string.Empty;
                    try
                    {
                        shortToken = _jwtService.GenerateToken(user);
                        shortRefreshToken = _jwtService.GenerateRefreshToken();
                        shortExpiresAt = DateTime.UtcNow.AddMinutes(60).ToString("o");
                    }
                    catch
                    {
                        // Ignore token generation errors; fallback to redirect without tokens
                    }

                    var redirectUrl = loginRedirectUrl1;
                    if (!string.IsNullOrEmpty(shortToken))
                    {
                        redirectUrl = $"{loginRedirectUrl1}?token={Uri.EscapeDataString(shortToken)}&refreshToken={Uri.EscapeDataString(shortRefreshToken)}&expiresAt={Uri.EscapeDataString(shortExpiresAt)}&next=dashboard";
                    }

                    // Safely serialize the redirect URL for embedding into JS to avoid quoting/escaping issues
                    var redirectUrlJson = JsonSerializer.Serialize(redirectUrl);

                    return Content($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Email Verification Status</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }}
                        .success {{ color: #4CAF50; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1 class='success'>Email Already Verified</h1>
                        <p>Your email is already verified. You will be redirected to the app shortly.</p>
                        <p id='message'>Redirecting in <span id='countdown' class='count'>3</span> seconds...</p>
                        <p><a id='manualLink' class='manual' href='{redirectUrl}'>Open in app now</a></p>
                    </div>
                    <script>
                        (function() {{
                            var countdownEl = document.getElementById('countdown');
                                var seconds = 3;
                                var redirectUrl = {redirectUrlJson};

                                var interval = setInterval(function() {{
                                seconds--;
                                if (countdownEl) countdownEl.textContent = seconds;
                                if (seconds <= 0) {{
                                    clearInterval(interval);
                                    try {{
                                        window.location.href = redirectUrl;
                                    }} catch (e) {{
                                        window.location.href = '/login';
                                    }}
                                }}
                            }}, 1000);
                        }})();
                    </script>
                </body>
                </html>
            ", "text/html");
                }

                // In a real app, validate the token against stored tokens
                // For now, we'll accept any token for simplicity
                user.IsEmailVerified = true;

                // Update EmailVerifiedAt if property exists
                try
                {
                    var emailVerifiedAtProp = typeof(User).GetProperty("EmailVerifiedAt");
                    if (emailVerifiedAtProp != null)
                    {
                        emailVerifiedAtProp.SetValue(user, DateTime.UtcNow);
                    }
                }
                catch
                {
                    // Ignore if property doesn't exist
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Email verified successfully for user: {user.Email}");

                // Assign ACCOUNT99 coupon to user after successful email verification
                try
                {
                    // Check if user can receive the ACCOUNT99 coupon (hasn't used it already)
                    if (await _userCouponService.CanUserReceiveCouponAsync(user.Email, user.PhoneNumber ?? ""))
                    {
                        var assignedCoupon = await _userCouponService.AssignPredefinedCouponAsync(user.Id, user.Email, user.PhoneNumber ?? "", "ACCOUNT99");
                        if (assignedCoupon != null)
                        {
                            var emailSent = await _userCouponService.SendPredefinedCouponEmailAsync(assignedCoupon, "ACCOUNT99");
                            if (emailSent)
                            {
                                _logger.LogInformation($"ACCOUNT99 coupon email sent successfully to {user.Email}");
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to send ACCOUNT99 coupon email to {user.Email}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to assign ACCOUNT99 coupon to user {user.Email}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"User {user.Email} already has used the account registration coupon, skipping assignment");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error assigning/sending ACCOUNT99 coupon for user {user.Email}");
                    // Don't fail the verification process if coupon assignment fails
                }

                // Generate tokens for auto-login and redirect to app
                var loginRedirect = _configuration["App:LoginRedirectUrl"] ?? "/login";
                string generatedToken = string.Empty;
                string generatedRefresh = string.Empty;
                string generatedExpires = string.Empty;
                try
                {
                    generatedToken = _jwtService.GenerateToken(user);
                    generatedRefresh = _jwtService.GenerateRefreshToken();
                    generatedExpires = DateTime.UtcNow.AddMinutes(60).ToString("o");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate tokens for redirect after verification");
                }

                var redirect = loginRedirect;
                if (!string.IsNullOrEmpty(generatedToken))
                {
                    redirect = $"{loginRedirect}?token={Uri.EscapeDataString(generatedToken)}&refreshToken={Uri.EscapeDataString(generatedRefresh)}&expiresAt={Uri.EscapeDataString(generatedExpires)}&next=dashboard";
                }

                // Safely serialize the redirect URL for embedding into JS to avoid quoting/escaping issues
                var redirectJson = JsonSerializer.Serialize(redirect);

                // Return success HTML with countdown and redirect
                return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Email Verification Success</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }}
                    .success {{ color: #4CAF50; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h1 class='success'>Email Verified Successfully!</h1>
                    <p>Your email has been verified. You will be redirected to the app and logged in automatically.</p>
                    <p id='message'>Redirecting to app in <span id='countdown' class='count'>3</span> seconds...</p>
                    <p><a id='manualLink' class='manual' href='{redirect}'>Open in app now</a></p>
                </div>
                <script>
                    (function() {{
                        var countdownEl = document.getElementById('countdown');
                            var seconds = 3;
                            var redirectUrl = {redirectJson};

                            var interval = setInterval(function() {{
                            seconds--;
                            if (countdownEl) countdownEl.textContent = seconds;
                            if (seconds <= 0) {{
                                clearInterval(interval);
                                try {{
                                    window.location.href = redirectUrl;
                                }} catch (e) {{
                                    window.location.href = '/login';
                                }}
                            }}
                        }}, 1000);
                    }})();
                </script>
            </body>
            </html>
        ", "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(500, "An error occurred while verifying your email. Please try again later.");
            }
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile(ProfileUpdateDto model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));
                }

                // Check if another user already has this phone number
                var existingUserWithPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber && u.Id != userId);
                    
                if (existingUserWithPhone != null)
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("Phone number is already in use"));
                }

                // Update user profile
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                if (!string.IsNullOrEmpty(model.ProfilePictureUrl))
                {
                    user.ProfilePictureUrl = model.ProfilePictureUrl;
                }
                
                await _context.SaveChangesAsync();

                // Return updated user
                return Ok(ApiResponse<UserDto>.SuccessResponse(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    IsKycVerified = user.IsKycVerified,
                    KycStatus = user.KycStatus,
                    AadhaarNumber = user.AadhaarNumber,
                    AadhaarImageUrl = user.AadhaarImageUrl,
                    PanNumber = user.PanNumber,
                    PanImageUrl = user.PanImageUrl,
                    KycSubmittedAt = user.KycSubmittedAt,
                    KycVerifiedAt = user.KycVerifiedAt,
                    KycRejectionReason = user.KycRejectionReason
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("An error occurred while updating profile"));
            }
        }

        [HttpPost("profile/image")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ProfileImageDto>>> UploadProfileImage(IFormFile profileImage)
        {
            try
            {
                _logger.LogInformation("=== PROFILE IMAGE UPLOAD STARTED (AZURE STORAGE) ===");
                _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("Request Content-Length: {ContentLength}", Request.ContentLength);
                
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("Profile image upload failed: Invalid token");
                    return Unauthorized(ApiResponse<ProfileImageDto>.ErrorResponse("Invalid token"));
                }

                _logger.LogInformation("Profile image upload for user ID: {UserId}", userId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Profile image upload failed: User not found for ID {UserId}", userId);
                    return NotFound(ApiResponse<ProfileImageDto>.ErrorResponse("User not found"));
                }

                _logger.LogInformation("User found: {UserEmail}", user.Email);

                if (profileImage == null || profileImage.Length == 0)
                {
                    _logger.LogWarning("Profile image upload failed: No image file provided");
                    return BadRequest(ApiResponse<ProfileImageDto>.ErrorResponse("No image file provided"));
                }

                _logger.LogInformation("Image file details - Name: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}", 
                    profileImage.FileName, profileImage.Length, profileImage.ContentType);

                // Check file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                
                _logger.LogInformation("File extension: {FileExtension}", fileExtension);
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning("Profile image upload failed: Invalid file type {FileExtension}", fileExtension);
                    return BadRequest(ApiResponse<ProfileImageDto>.ErrorResponse("Invalid file type. Only JPG, JPEG, and PNG files are allowed."));
                }

                // Generate a unique file name
                var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
                _logger.LogInformation("Generated file name: {FileName}", fileName);

                // Upload new file and delete old one atomically using the file service
                _logger.LogInformation("Starting profile image update with old file deletion...");
                var imageUrl = await _fileService.UpdateProfileImageAsync(profileImage, user.ProfilePictureUrl, "profile-images");
                _logger.LogInformation("Profile image updated successfully. URL: {ImageUrl}", imageUrl);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogError("Failed to upload profile image - empty URL returned");
                    return StatusCode(500, ApiResponse<ProfileImageDto>.ErrorResponse("Failed to upload profile image"));
                }

                // Update user profile image in database
                _logger.LogInformation("Updating user profile image in database...");
                user.ProfilePictureUrl = imageUrl;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Database updated successfully");

                _logger.LogInformation("=== PROFILE IMAGE UPLOAD COMPLETED SUCCESSFULLY (AZURE STORAGE) ===");

                return Ok(ApiResponse<ProfileImageDto>.SuccessResponse(new ProfileImageDto
                {
                    ImageUrl = imageUrl
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== PROFILE IMAGE UPLOAD ERROR === {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, ApiResponse<ProfileImageDto>.ErrorResponse("An error occurred while uploading profile image"));
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<UserDto>.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));
                }

                return Ok(ApiResponse<UserDto>.SuccessResponse(new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    IsKycVerified = user.IsKycVerified,
                    KycStatus = user.KycStatus,
                    AadhaarNumber = user.AadhaarNumber,
                    AadhaarImageUrl = user.AadhaarImageUrl,
                    PanNumber = user.PanNumber,
                    PanImageUrl = user.PanImageUrl,
                    KycSubmittedAt = user.KycSubmittedAt,
                    KycVerifiedAt = user.KycVerifiedAt,
                    KycRejectionReason = user.KycRejectionReason
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("An error occurred while retrieving profile"));
            }
        }

        // Private method to sanitize input to prevent XSS attacks
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Basic sanitization
            input = input.Trim();
            input = input.Replace("<", "&lt;").Replace(">", "&gt;");
            return input;
        }

        // Helper methods
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Simple regex for email validation
            try
            {
                // Use built-in .NET email validation
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove all non-digit characters for validation
            var digitsOnly = new Regex(@"[^\d]").Replace(phoneNumber, "");

            // Most phone numbers are between 8 and 15 digits
            return digitsOnly.Length >= 8 && digitsOnly.Length <= 15;
        }

        private List<string> ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password cannot be empty");
                return errors;
            }

            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters long");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one number");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add("Password must contain at least one special character");

            return errors;
        }

        // This duplicate method was removed to fix the ambiguous call error

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(ForgotPasswordDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Email is required"));
                }

                // Check if user exists
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted);

                // For security reasons, always return success even if user doesn't exist
                // This prevents email enumeration attacks
                if (user != null)
                {
                    // Generate a secure reset token
                    var resetToken = _passwordService.GenerateSecureToken();
                    var resetLink = $"{Request.Scheme}://{Request.Host}/api/auth/direct-reset-password?email={Uri.EscapeDataString(user.Email)}&token={resetToken}";
                    
                    try
                    {
                        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
                        _logger.LogInformation($"Password reset email sent to {user.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Failed to send password reset email to {user.Email}");
                        // Don't return error to user for security reasons
                    }
                }                return Ok(ApiResponse<object>.SuccessResponse(new object(), 
                    "If an account with that email exists, a password reset link has been sent to your email address."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred. Please try again later."));
            }
        }

        [HttpGet("debug/check-user/{email}")]
        public async Task<ActionResult> CheckUser(string email)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted)
                    .Select(u => new { u.Email, u.Role, u.IsDeleted, u.IsEmailVerified })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Ok(new { exists = false, message = "User not found" });
                }

                return Ok(new { exists = true, user = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("check-email-status/{email}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> CheckEmailStatus(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Email is required"));
                }

                var user = await _context.Users
                    .Where(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted)
                    .Select(u => new { u.Email, u.Role, u.IsDeleted, u.IsEmailVerified })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { 
                        exists = false, 
                        isVerified = false,
                        status = "not_registered"
                    }, "User not found"));
                }

                var status = user.IsEmailVerified ? "verified" : "not_verified";
                return Ok(ApiResponse<object>.SuccessResponse(new { 
                    exists = true, 
                    isVerified = user.IsEmailVerified,
                    status = status,
                    user = new {
                        email = user.Email,
                        role = user.Role,
                        isEmailVerified = user.IsEmailVerified
                    }
                }, "Email status retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while checking email status"));
            }
        }

        [HttpGet("check-phone-status/{phone}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> CheckPhoneStatus(string phone)
        {
            try
            {
                if (string.IsNullOrEmpty(phone))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Phone number is required"));
                }

                // Clean the phone number to handle different formats
                var cleanPhone = Regex.Replace(phone, @"[^\d+]", "");

                var user = await _context.Users
                    .Where(u => u.PhoneNumber == cleanPhone && !u.IsDeleted)
                    .Select(u => new { u.PhoneNumber, u.Role, u.IsDeleted })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { 
                        exists = false, 
                        status = "not_registered"
                    }, "Phone number not registered"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(new { 
                    exists = true, 
                    status = "registered",
                    user = new {
                        phoneNumber = user.PhoneNumber,
                        role = user.Role
                    }
                }, "Phone number is already registered"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking phone number status");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while checking phone number status"));
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users
                    .Where(u => u.Id == currentUserId && !u.IsDeleted)
                    .Select(u => new {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.PhoneNumber,
                        u.Role,
                        u.IsEmailVerified,
                        u.ProfilePictureUrl,
                        u.CreatedAt,
                        u.LastLoginDate
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(user, "User data retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user data");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while retrieving user data"));
            }
        }

        private int? GetCurrentUserId()
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return null;
            }

            // Get user id from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return userId;
        }



        [HttpGet("debug/photo-urls")]
        [AllowAnonymous]
        public ActionResult<object> DebugPhotoUrls()
        {
            var configuredBaseUrl = _configuration["FileStorage:BaseUrl"] ?? "/uploads";
            var testFileName = "test-profile-image.jpg";
            var relativeUrl = $"{configuredBaseUrl}/profile-images/{testFileName}";
            var fullUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
            
            return Ok(new
            {
                success = true,
                message = "Photo URL debug information",
                data = new
                {
                    environment = _environment.EnvironmentName,
                    requestScheme = Request.Scheme,
                    requestHost = Request.Host.ToString(),
                    configuredBaseUrl = configuredBaseUrl,
                    testFileName = testFileName,
                    generatedRelativeUrl = relativeUrl,
                    generatedFullUrl = fullUrl,
                    fileStorageConfig = new
                    {
                        localPath = _configuration["FileStorage:LocalPath"],
                        baseUrl = _configuration["FileStorage:BaseUrl"]
                    }
                }
            });
        }
    }

    public class RefreshTokenRequestDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ResendVerificationDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ProfileImageDto
    {
        public string ImageUrl { get; set; } = string.Empty;
    }
}

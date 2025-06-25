using Google.Apis.Auth;
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
        private readonly IOptions<GoogleAuthSettings> _googleAuthSettings;

        public AuthController(
            ApplicationDbContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IOptions<GoogleAuthSettings> googleAuthSettings,
            IWebHostEnvironment environment)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
            _googleAuthSettings = googleAuthSettings;
            _environment = environment;
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

        [HttpPost("google-sign-in")]
        public async Task<ActionResult<ApiResponse<ExternalAuthResponseDto>>> GoogleSignIn(GoogleAuthRequestDto request)
        {
            try
            {
                // Validate the Google ID token
                GoogleJsonWebSignature.Payload payload;
                try
                {
                    payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _googleAuthSettings.Value.ClientId }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invalid Google token");
                    return BadRequest(ApiResponse<ExternalAuthResponseDto>.ErrorResponse("Invalid Google token"));
                }

                // Check if the user already exists
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == payload.Email.ToLower() && !u.IsDeleted);

                bool isNewUser = false;

                // If user doesn't exist, create a new account
                if (user == null)
                {
                    isNewUser = true;

                    // Generate a secure random password (user won't need this)
                    var securePassword = _passwordService.GenerateSecureToken();
                    var passwordHash = _passwordService.HashPassword(securePassword);

                    // Extract first and last name from Google profile
                    var nameParts = payload.Name.Split(' ');
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                    var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                    user = new User
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = payload.Email,
                        PhoneNumber = "", // Google doesn't provide phone number
                        PasswordHash = passwordHash,
                        Role = "Customer", // Default role for new users
                        IsEmailVerified = payload.EmailVerified, // Google has already verified the email
                        ExternalAuthProvider = "Google",
                        ExternalAuthId = payload.Subject, // Google's unique user ID
                        ProfilePictureUrl = payload.Picture // Add profile picture from Google 
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"New user registered via Google: {user.Email}");
                }
                else
                {
                    // Update existing user with Google information if they're logging in with Google for the first time
                    if (string.IsNullOrEmpty(user.ExternalAuthProvider))
                    {
                        user.ExternalAuthProvider = "Google";
                        user.ExternalAuthId = payload.Subject;

                        // If the user hasn't verified their email through our system but Google has verified it
                        if (!user.IsEmailVerified && payload.EmailVerified)
                        {
                            user.IsEmailVerified = true;
                        }

                        // Update profile picture if not set
                        if (string.IsNullOrEmpty(user.ProfilePictureUrl) && !string.IsNullOrEmpty(payload.Picture))
                        {
                            user.ProfilePictureUrl = payload.Picture;
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(60);

                // Update last login info if properties exist
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

                var response = new ExternalAuthResponseDto
                {
                    IsNewUser = isNewUser,
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

                string welcomeMessage = isNewUser
                    ? $"Welcome to Stibe, {user.FirstName}! Your account has been created successfully."
                    : $"Welcome back, {user.FirstName}!";

                _logger.LogInformation($"User logged in via Google: {user.Email}");
                return Ok(ApiResponse<ExternalAuthResponseDto>.SuccessResponse(response, welcomeMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google sign-in");
                return StatusCode(500, ApiResponse<ExternalAuthResponseDto>.ErrorResponse("An error occurred during Google sign-in. Please try again later."));
            }
        }

        [HttpGet("debug-google-auth")]
        [AllowAnonymous]
        public ActionResult DebugGoogleAuth()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("Debug endpoint only available in development");
            }

            try
            {
                var debugInfo = new
                {
                    GoogleAuth = new
                    {
                        _googleAuthSettings.Value.ClientId,
                        ClientIdPresent = !string.IsNullOrEmpty(_googleAuthSettings.Value.ClientId),
                        ClientSecretLength = _googleAuthSettings.Value.ClientSecret?.Length ?? 0
                    },
                    Environment = _environment.EnvironmentName,
                    RequestDetails = new
                    {
                        Request.Scheme,
                        Host = Request.Host.ToString(),
                        PathBase = Request.PathBase.ToString(),
                        Path = Request.Path.ToString()
                    }
                };

                return Ok(debugInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Debug error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [HttpPost("debug-token")]
        [AllowAnonymous]
        public async Task<ActionResult> DebugToken([FromBody] GoogleAuthRequestDto request)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("Debug endpoint only available in development");
            }

            try
            {
                var debugResponse = new Dictionary<string, object>();
                debugResponse["tokenReceived"] = !string.IsNullOrEmpty(request.IdToken);
                debugResponse["tokenFirstChars"] = request.IdToken?.Substring(0, Math.Min(10, request.IdToken?.Length ?? 0)) + "...";
                debugResponse["tokenLength"] = request.IdToken?.Length ?? 0;

                try
                {
                    var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _googleAuthSettings.Value.ClientId }
                    };
                    debugResponse["validationSettings"] = JsonSerializer.Serialize(validationSettings);

                    var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
                    debugResponse["validationSuccess"] = true;
                    debugResponse["email"] = payload.Email;
                    debugResponse["name"] = payload.Name;
                    debugResponse["picture"] = payload.Picture;
                    debugResponse["emailVerified"] = payload.EmailVerified;
                }
                catch (Exception tokenEx)
                {
                    debugResponse["validationSuccess"] = false;
                    debugResponse["validationError"] = tokenEx.Message;
                    debugResponse["validationErrorType"] = tokenEx.GetType().Name;
                    debugResponse["validationErrorStack"] = tokenEx.StackTrace;
                }

                return Ok(debugResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Debug error: {ex.Message}\n{ex.StackTrace}");
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
                if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length < 2)
                {
                    validationErrors.Add("First name must be at least 2 characters");
                }

                // Make LastName optional but if provided, must be at least 2 characters
                if (!string.IsNullOrWhiteSpace(request.LastName) && request.LastName.Length < 2)
                {
                    validationErrors.Add("Last name must be at least 2 characters when provided");
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
                if (request.Role != "Customer" && request.Role != "SalonOwner")
                {
                    validationErrors.Add("Invalid role. Must be 'Customer' or 'SalonOwner'");
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
                        CreatedAt = user.CreatedAt
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
                        <a href='/login'>Go to Login</a>
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

                return Content($@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Password Reset Success</title>
                <style>
                    body {{ font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }}
                    .success {{ color: #4CAF50; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h1 class='success'>Password Reset Successfully!</h1>
                    <p>Your password has been reset. You can now log in with your new password.</p>
                    <p>Redirecting to login page in 3 seconds...</p>
                </div>
                <script>
                    setTimeout(function() {{
                        window.location.href = '/login';
                    }}, 3000);
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
                // In a production app, validate the refresh token against stored tokens
                var userId = _jwtService.GetUserIdFromToken(request.Token);
                if (userId == null)
                {
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Invalid token"));
                }

                var user = await _context.Users
                    .Where(u => u.Id == userId && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
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

                _logger.LogInformation($"Token refreshed for user: {user.Email}");
                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, "Token refreshed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse("An error occurred"));
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
                    // Return success HTML - already verified
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
                        <p>Your email is already verified. You can now log in to your account.</p>
                        <p>You can close this window and return to the app to continue.</p>
                    </div>
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

                // Return success HTML
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
                    <p>Your email has been verified. You can now log in to your account.</p>
                    <p>You can close this window and return to the app to continue.</p>
                </div>
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

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        #region Helper Methods

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use regex pattern for email validation
                var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                return Regex.IsMatch(email, pattern);
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

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove potentially dangerous characters
            return Regex.Replace(input.Trim(), @"[<>&'""]", "");        }

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
                    .Where(u => u.Email.ToLower() == email.ToLower())
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

        #endregion
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
}

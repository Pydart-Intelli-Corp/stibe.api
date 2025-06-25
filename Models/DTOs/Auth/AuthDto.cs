using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.Auth
{
    public class AdminRegistrationDto : RegisterRequestDto
    {
        public bool IsSystemAdmin { get; set; } = false;
        public bool CanMonitorSalons { get; set; } = true;
        public bool CanMonitorStaff { get; set; } = true;
        public bool CanMonitorBookings { get; set; } = true;
        public bool CanMonitorUsers { get; set; } = true;
        public bool CanModifySystemSettings { get; set; } = false;

        // This will ensure the Role is always set to "Admin"
        public new string Role { get; set; } = "Admin";
    }
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
    public class GoogleAuthRequestDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    public class ExternalAuthResponseDto
    {
        public bool IsNewUser { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class ResendVerificationRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }
    public class CompleteResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RegisterRequestDto
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Customer";
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }
    public class AdminUserDto : UserDto
    {
        public bool IsSystemAdmin { get; set; }
        public bool CanMonitorSalons { get; set; }
        public bool CanMonitorStaff { get; set; }
        public bool CanMonitorBookings { get; set; }
        public bool CanMonitorUsers { get; set; }
        public bool CanModifySystemSettings { get; set; }
        public DateTime? AdminRoleAssignedDate { get; set; }
    }
    public class UserDto
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

    public class ChangePasswordRequestDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string ResetToken { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
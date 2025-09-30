using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.Otp
{
    public class SendOtpRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Purpose { get; set; } = string.Empty;
    }

    public class VerifyOtpRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(6)]
        [MinLength(6)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Purpose { get; set; } = string.Empty;
    }

    public class OtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int? AttemptsRemaining { get; set; }
        public DateTime? NextAllowedAt { get; set; }
    }

    public class OtpStatusDto
    {
        public bool HasPendingOtp { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int AttemptsRemaining { get; set; }
        public DateTime? NextAllowedAt { get; set; }
        public bool CanRequestNew { get; set; }
    }
}

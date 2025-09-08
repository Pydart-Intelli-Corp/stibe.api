using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.Entities
{
    public class OtpEntity : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(6)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public int AttemptCount { get; set; } = 0;

        public DateTime? LastAttemptAt { get; set; }

        // Purpose types as constants
        public const string PURPOSE_EMAIL_VERIFICATION = "EMAIL_VERIFICATION";
        public const string PURPOSE_SHOP_ACCESS = "SHOP_ACCESS";
        public const string PURPOSE_PROFILE_ACCESS = "PROFILE_ACCESS";
        public const string PURPOSE_SHOP_STATUS_CHANGE = "SHOP_STATUS_CHANGE";
        public const string PURPOSE_SHOP_DEFAULT_CHANGE = "SHOP_DEFAULT_CHANGE";
        public const string PURPOSE_SHOP_DELETE = "SHOP_DELETE";
        public const string PURPOSE_PASSWORD_RESET = "PASSWORD_RESET";
        public const string PURPOSE_PHONE_VERIFICATION = "PHONE_VERIFICATION";
        public const string PURPOSE_TWO_FACTOR_AUTH = "TWO_FACTOR_AUTH";
    }
}

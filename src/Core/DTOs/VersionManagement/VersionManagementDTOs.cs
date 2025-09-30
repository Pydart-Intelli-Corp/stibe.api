using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.VersionManagement
{
    /// <summary>
    /// Request DTO for checking app updates
    /// </summary>
    public class CheckUpdateRequestDto
    {
        [Required]
        [StringLength(20)]
        public string CurrentVersion { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Platform { get; set; } = string.Empty; // "Android" only

        [StringLength(50)]
        public string? DeviceModel { get; set; }

        [StringLength(20)]
        public string? OsVersion { get; set; }

        [StringLength(100)]
        public string? AppBundleId { get; set; }

        public int? BuildNumber { get; set; }
    }

    /// <summary>
    /// Response DTO for update check
    /// </summary>
    public class CheckUpdateResponseDto
    {
        public bool UpdateAvailable { get; set; }
        public bool IsForceUpdate { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string? MinRequiredVersion { get; set; }
        public string UpdateMessage { get; set; } = string.Empty;
        public List<string> ReleaseNotes { get; set; } = new();
        public string? UpdateUrl { get; set; }
        public string? UpdateSize { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }

    /// <summary>
    /// DTO for recording update completion
    /// </summary>
    public class UpdateCompletionDto
    {
        [Required]
        [StringLength(20)]
        public string FromVersion { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ToVersion { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Platform { get; set; } = string.Empty; // "Android" only

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool UpdateSuccessful { get; set; } = true;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for changelog information
    /// </summary>
    public class ChangelogDto
    {
        public string Version { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public List<string> Changes { get; set; } = new();
        public bool IsLatest { get; set; }
    }

    /// <summary>
    /// DTO for server information
    /// </summary>
    public class ServerInfoDto
    {
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public DateTime BuildDate { get; set; }
        public string ApiVersion { get; set; } = string.Empty;
        public List<string> SupportedAppVersions { get; set; } = new();
    }
}
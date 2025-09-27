using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using stibe.api.Models.DTOs.VersionManagement;

namespace stibe.api.Controllers
{
    /// <summary>
    /// Version management and app update controller
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<VersionController> _logger;
        private readonly IConfiguration _configuration;

        public VersionController(ILogger<VersionController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Check for app updates
        /// </summary>
        /// <param name="request">Current app version info</param>
        /// <returns>Update information</returns>
        [HttpPost("check-update")]
        public async Task<IActionResult> CheckForUpdate([FromBody] CheckUpdateRequestDto request)
        {
            try
            {
                _logger.LogInformation("Checking for updates. Current version: {CurrentVersion}, Platform: {Platform}", 
                    request.CurrentVersion, request.Platform);

                // Get latest version info from configuration or database
                var updateInfo = await GetUpdateInfo(request.Platform);

                if (updateInfo == null)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "No update information available",
                        data = new CheckUpdateResponseDto
                        {
                            UpdateAvailable = false,
                            IsForceUpdate = false,
                            CurrentVersion = request.CurrentVersion,
                            LatestVersion = request.CurrentVersion,
                            UpdateMessage = "You are using the latest version"
                        }
                    });
                }

                // Compare versions
                var isUpdateAvailable = IsUpdateRequired(request.CurrentVersion, updateInfo.LatestVersion);
                var isForceUpdate = IsForceUpdateRequired(request.CurrentVersion, updateInfo.MinRequiredVersion);

                var response = new CheckUpdateResponseDto
                {
                    UpdateAvailable = isUpdateAvailable,
                    IsForceUpdate = isForceUpdate,
                    CurrentVersion = request.CurrentVersion,
                    LatestVersion = updateInfo.LatestVersion,
                    MinRequiredVersion = updateInfo.MinRequiredVersion,
                    UpdateMessage = isForceUpdate ? updateInfo.ForceUpdateMessage : updateInfo.OptionalUpdateMessage,
                    ReleaseNotes = updateInfo.ReleaseNotes,
                    UpdateUrl = GetUpdateUrl(request.Platform, updateInfo),
                    UpdateSize = updateInfo.UpdateSize,
                    ReleaseDate = updateInfo.ReleaseDate
                };

                _logger.LogInformation("Update check result: UpdateAvailable={UpdateAvailable}, IsForceUpdate={IsForceUpdate}", 
                    isUpdateAvailable, isForceUpdate);

                return Ok(new
                {
                    success = true,
                    message = isUpdateAvailable ? "Update available" : "App is up to date",
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to check for updates",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get app changelog
        /// </summary>
        /// <param name="version">Optional version to get changelog for</param>
        /// <returns>Changelog information</returns>
        [HttpGet("changelog")]
        public async Task<IActionResult> GetChangelog([FromQuery] string? version = null)
        {
            try
            {
                var changelog = await GetChangelogInfo(version);
                
                return Ok(new
                {
                    success = true,
                    message = "Changelog retrieved successfully",
                    data = changelog
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting changelog");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get changelog",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Record app update completion (for analytics)
        /// </summary>
        /// <param name="request">Update completion info</param>
        /// <returns>Success response</returns>
        [HttpPost("update-completed")]
        [Authorize]
        public async Task<IActionResult> RecordUpdateCompletion([FromBody] UpdateCompletionDto request)
        {
            try
            {
                _logger.LogInformation("Update completed. User: {UserId}, From: {FromVersion}, To: {ToVersion}", 
                    User.Identity?.Name, request.FromVersion, request.ToVersion);

                // Here you could save update analytics to database
                // For now, just log the information

                return Ok(new
                {
                    success = true,
                    message = "Update completion recorded",
                    data = new { recordedAt = DateTime.UtcNow }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording update completion");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to record update completion",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get current server version and build info
        /// </summary>
        /// <returns>Server version information</returns>
        [HttpGet("server-info")]
        public IActionResult GetServerInfo()
        {
            try
            {
                var serverInfo = new ServerInfoDto
                {
                    Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
                    Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                    BuildDate = GetBuildDate(),
                    ApiVersion = "1.0",
                    SupportedAppVersions = GetSupportedAppVersions()
                };

                return Ok(new
                {
                    success = true,
                    message = "Server information retrieved",
                    data = serverInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting server info");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get server info",
                    errors = new[] { ex.Message }
                });
            }
        }

        #region Private Methods

        /// <summary>
        /// Get update information for a platform
        /// </summary>
        private async Task<UpdateInfo?> GetUpdateInfo(string platform)
        {
            // In a real application, this would come from a database
            // For now, we'll use configuration-based approach
            var updateConfig = _configuration.GetSection("AppUpdates");
            
            return await Task.FromResult(new UpdateInfo
            {
                LatestVersion = updateConfig[$"{platform}:LatestVersion"] ?? "1.0.0",
                MinRequiredVersion = updateConfig[$"{platform}:MinRequiredVersion"] ?? "1.0.0",
                OptionalUpdateMessage = updateConfig[$"{platform}:OptionalUpdateMessage"] ?? "A new version is available with improvements and bug fixes.",
                ForceUpdateMessage = updateConfig[$"{platform}:ForceUpdateMessage"] ?? "This version is no longer supported. Please update to continue using the app.",
                ReleaseNotes = GetReleaseNotes(updateConfig[$"{platform}:LatestVersion"] ?? "1.0.0"),
                UpdateSize = updateConfig[$"{platform}:UpdateSize"] ?? "25 MB",
                ReleaseDate = DateTime.TryParse(updateConfig[$"{platform}:ReleaseDate"], out var date) ? date : DateTime.UtcNow,
                DownloadUrl = updateConfig[$"{platform}:DownloadUrl"] ?? "",
                PlayStoreUrl = updateConfig["Android:PlayStoreUrl"] ?? ""
            });
        }

        /// <summary>
        /// Check if update is required
        /// </summary>
        private bool IsUpdateRequired(string currentVersion, string latestVersion)
        {
            try
            {
                var current = new Version(NormalizeVersion(currentVersion));
                var latest = new Version(NormalizeVersion(latestVersion));
                return current < latest;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if force update is required
        /// </summary>
        private bool IsForceUpdateRequired(string currentVersion, string minRequiredVersion)
        {
            try
            {
                var current = new Version(NormalizeVersion(currentVersion));
                var minRequired = new Version(NormalizeVersion(minRequiredVersion));
                return current < minRequired;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Normalize version string for comparison
        /// </summary>
        private string NormalizeVersion(string version)
        {
            // Remove build number if present (e.g., "1.0.0+2" -> "1.0.0")
            if (version.Contains('+'))
            {
                version = version.Split('+')[0];
            }

            // Ensure we have at least 3 parts (major.minor.patch)
            var parts = version.Split('.');
            while (parts.Length < 3)
            {
                version += ".0";
                parts = version.Split('.');
            }

            return version;
        }

        /// <summary>
        /// Get update URL for platform
        /// </summary>
        private string GetUpdateUrl(string platform, UpdateInfo updateInfo)
        {
            return platform.ToLower() switch
            {
                "android" => updateInfo.DownloadUrl, // Always use direct download URL for Android
                _ => updateInfo.DownloadUrl
            };
        }

        /// <summary>
        /// Download APK file directly
        /// </summary>
        /// <param name="version">APK version to download</param>
        /// <returns>APK file</returns>
        [HttpGet("download-apk/{version}")]
        public async Task<IActionResult> DownloadApk(string version)
        {
            try
            {
                var updateConfig = _configuration.GetSection("AppUpdates:Android");
                var apkFileName = updateConfig["ApkFileName"] ?? $"stibe-v{version}.apk";
                var apkPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apk", apkFileName);

                _logger.LogInformation("APK download requested for version: {Version}, Path: {Path}", version, apkPath);

                if (!System.IO.File.Exists(apkPath))
                {
                    _logger.LogWarning("APK file not found: {Path}", apkPath);
                    return NotFound(new
                    {
                        success = false,
                        message = $"APK file for version {version} not found",
                        errors = new[] { "The requested APK file does not exist on the server" }
                    });
                }

                var fileInfo = new FileInfo(apkPath);
                var fileStream = new FileStream(apkPath, FileMode.Open, FileAccess.Read);

                _logger.LogInformation("Serving APK file: {FileName}, Size: {Size} bytes", apkFileName, fileInfo.Length);

                return File(fileStream, "application/vnd.android.package-archive", apkFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading APK for version: {Version}", version);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to download APK",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get list of available APK files
        /// </summary>
        /// <returns>List of available APK versions</returns>
        [HttpGet("available-apks")]
        public IActionResult GetAvailableApks()
        {
            try
            {
                var apkDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apk");
                
                if (!Directory.Exists(apkDirectory))
                {
                    Directory.CreateDirectory(apkDirectory);
                    return Ok(new
                    {
                        success = true,
                        message = "No APK files available",
                        data = new List<object>()
                    });
                }

                var apkFiles = Directory.GetFiles(apkDirectory, "*.apk")
                    .Select(filePath => new
                    {
                        fileName = Path.GetFileName(filePath),
                        version = ExtractVersionFromFileName(Path.GetFileName(filePath)),
                        size = new FileInfo(filePath).Length,
                        lastModified = new FileInfo(filePath).LastWriteTime,
                        downloadUrl = $"{Request.Scheme}://{Request.Host}/apk/{Path.GetFileName(filePath)}"
                    })
                    .OrderByDescending(x => x.lastModified)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = $"Found {apkFiles.Count} APK files",
                    data = apkFiles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available APKs");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get available APKs",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Upload new APK file (for admin use)
        /// </summary>
        /// <param name="apkFile">APK file to upload</param>
        /// <param name="version">Version number</param>
        /// <returns>Upload result</returns>
        [HttpPost("upload-apk")]
        public async Task<IActionResult> UploadApk(IFormFile apkFile, [FromForm] string version)
        {
            try
            {
                if (apkFile == null || apkFile.Length == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No APK file provided",
                        errors = new[] { "APK file is required" }
                    });
                }

                if (string.IsNullOrEmpty(version))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Version is required",
                        errors = new[] { "Version parameter is required" }
                    });
                }

                // Validate file extension
                if (!apkFile.FileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid file type",
                        errors = new[] { "Only APK files are allowed" }
                    });
                }

                var apkDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apk");
                Directory.CreateDirectory(apkDirectory);

                var fileName = $"stibe-v{version}.apk";
                var filePath = Path.Combine(apkDirectory, fileName);

                _logger.LogInformation("Uploading APK: {FileName}, Size: {Size} bytes", fileName, apkFile.Length);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await apkFile.CopyToAsync(stream);
                }

                var downloadUrl = $"{Request.Scheme}://{Request.Host}/apk/{fileName}";

                _logger.LogInformation("APK uploaded successfully: {FilePath}", filePath);

                return Ok(new
                {
                    success = true,
                    message = "APK uploaded successfully",
                    data = new
                    {
                        fileName = fileName,
                        version = version,
                        size = apkFile.Length,
                        downloadUrl = downloadUrl,
                        uploadedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading APK");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to upload APK",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get release notes for version
        /// </summary>
        private List<string> GetReleaseNotes(string version)
        {
            // In a real application, this would come from a database
            // For demo purposes, returning sample notes
            return new List<string>
            {
                "🎉 New features and improvements",
                "🐛 Bug fixes and performance enhancements",
                "🔧 Updated user interface",
                "⚡ Faster app startup time",
                "🛡️ Enhanced security measures"
            };
        }

        /// <summary>
        /// Get changelog information
        /// </summary>
        private async Task<ChangelogDto> GetChangelogInfo(string? version)
        {
            // In a real application, this would come from a database
            var changelog = new ChangelogDto
            {
                Version = version ?? "1.0.0",
                ReleaseDate = DateTime.UtcNow,
                Changes = GetReleaseNotes(version ?? "1.0.0"),
                IsLatest = string.IsNullOrEmpty(version)
            };

            return await Task.FromResult(changelog);
        }

        /// <summary>
        /// Get build date from assembly
        /// </summary>
        private DateTime GetBuildDate()
        {
            try
            {
                var assembly = GetType().Assembly;
                var fileInfo = new System.IO.FileInfo(assembly.Location);
                return fileInfo.CreationTime;
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Get supported app versions
        /// </summary>
        private List<string> GetSupportedAppVersions()
        {
            var updateConfig = _configuration.GetSection("AppUpdates");
            var supportedVersions = updateConfig.GetSection("SupportedVersions").Get<List<string>>();
            return supportedVersions ?? new List<string> { "1.0.0" };
        }

        /// <summary>Extract version information from APK filename</summary>
        private string ExtractVersionFromFileName(string fileName)
        {
            try
            {
                // Expected format: appname-v1.2.3.apk or appname_1.2.3.apk
                var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
                
                // Try different patterns
                var patterns = new[]
                {
                    @"-v(\d+\.\d+\.\d+)", // appname-v1.2.3
                    @"_v(\d+\.\d+\.\d+)", // appname_v1.2.3
                    @"-(\d+\.\d+\.\d+)",  // appname-1.2.3
                    @"_(\d+\.\d+\.\d+)",  // appname_1.2.3
                    @"(\d+\.\d+\.\d+)"    // any version pattern
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(withoutExtension, pattern);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }

                // If no pattern matches, return filename without extension
                return withoutExtension;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to extract version from filename {FileName}: {Error}", fileName, ex.Message);
                return "unknown";
            }
        }

        /// <summary>Format file size for display</summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        #endregion

        #region Helper Classes

        private class UpdateInfo
        {
            public string LatestVersion { get; set; } = string.Empty;
            public string MinRequiredVersion { get; set; } = string.Empty;
            public string OptionalUpdateMessage { get; set; } = string.Empty;
            public string ForceUpdateMessage { get; set; } = string.Empty;
            public List<string> ReleaseNotes { get; set; } = new();
            public string UpdateSize { get; set; } = string.Empty;
            public DateTime ReleaseDate { get; set; }
            public string DownloadUrl { get; set; } = string.Empty;
            public string PlayStoreUrl { get; set; } = string.Empty;
        }

        #endregion
    }
}
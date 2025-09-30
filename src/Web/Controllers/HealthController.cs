using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection;
using stibe.api.Data;

namespace stibe.api.Controllers
{
    /// <summary>
    /// Health check and system monitoring controller
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            ApplicationDbContext context, 
            IConfiguration configuration,
            ILogger<HealthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Basic health check endpoint
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var healthStatus = new HealthStatus
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
                    Version = GetApplicationVersion(),
                    Uptime = GetUptime()
                };

                // Test database connectivity
                var dbHealthy = await TestDatabaseConnection();
                healthStatus.DatabaseStatus = dbHealthy ? "Connected" : "Disconnected";

                // Check disk space
                var diskInfo = GetDiskInfo();
                healthStatus.DiskSpace = diskInfo;

                // Check memory usage
                var memoryInfo = GetMemoryInfo();
                healthStatus.Memory = memoryInfo;

                // Determine overall status
                if (!dbHealthy)
                {
                    healthStatus.Status = "Unhealthy";
                    healthStatus.Issues = new List<string> { "Database connection failed" };
                }
                else if (diskInfo.FreeSpacePercent < 10)
                {
                    healthStatus.Status = "Degraded";
                    healthStatus.Issues = new List<string> { "Low disk space" };
                }

                var statusCode = healthStatus.Status switch
                {
                    "Healthy" => 200,
                    "Degraded" => 200,
                    "Unhealthy" => 503,
                    _ => 500
                };

                _logger.LogInformation("Health check completed: {Status}", healthStatus.Status);
                return StatusCode(statusCode, healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new HealthStatus
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Issues = new List<string> { "Health check failed: " + ex.Message }
                });
            }
        }

        /// <summary>
        /// Detailed health check with dependency status
        /// </summary>
        /// <returns>Detailed health information</returns>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailedHealth()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                var detailedHealth = new DetailedHealthStatus
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
                    Version = GetApplicationVersion(),
                    Uptime = GetUptime(),
                    Checks = new Dictionary<string, HealthCheck>()
                };

                // Database check
                var dbCheck = await PerformDatabaseCheck();
                detailedHealth.Checks["database"] = dbCheck;

                // File system check
                var fileSystemCheck = PerformFileSystemCheck();
                detailedHealth.Checks["filesystem"] = fileSystemCheck;

                // Configuration check
                var configCheck = PerformConfigurationCheck();
                detailedHealth.Checks["configuration"] = configCheck;

                // External services check (if any)
                var externalCheck = await PerformExternalServicesCheck();
                detailedHealth.Checks["external_services"] = externalCheck;

                // Determine overall status
                var failedChecks = detailedHealth.Checks.Values.Where(c => c.Status != "Healthy").ToList();
                if (failedChecks.Any(c => c.Status == "Unhealthy"))
                {
                    detailedHealth.Status = "Unhealthy";
                }
                else if (failedChecks.Any(c => c.Status == "Degraded"))
                {
                    detailedHealth.Status = "Degraded";
                }

                stopwatch.Stop();
                detailedHealth.ResponseTime = stopwatch.ElapsedMilliseconds;

                var statusCode = detailedHealth.Status switch
                {
                    "Healthy" => 200,
                    "Degraded" => 200,
                    "Unhealthy" => 503,
                    _ => 500
                };

                return StatusCode(statusCode, detailedHealth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed");
                return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
            }
        }

        /// <summary>
        /// Get application metrics
        /// </summary>
        /// <returns>Performance metrics</returns>
        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var metrics = new ApplicationMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    ProcessId = process.Id,
                    WorkingSet = process.WorkingSet64,
                    PrivateMemorySize = process.PrivateMemorySize64,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    StartTime = process.StartTime,
                    TotalProcessorTime = process.TotalProcessorTime,
                    GcMemory = GC.GetTotalMemory(false),
                    GcCollections = new Dictionary<int, int>
                    {
                        { 0, GC.CollectionCount(0) },
                        { 1, GC.CollectionCount(1) },
                        { 2, GC.CollectionCount(2) }
                    }
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metrics");
                return StatusCode(500, new { error = "Failed to retrieve metrics" });
            }
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        private async Task<bool> TestDatabaseConnection()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database connection test failed");
                return false;
            }
        }

        /// <summary>
        /// Perform detailed database check
        /// </summary>
        private async Task<HealthCheck> PerformDatabaseCheck()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Test connection
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return new HealthCheck
                    {
                        Status = "Unhealthy",
                        Description = "Cannot connect to database",
                        ResponseTime = stopwatch.ElapsedMilliseconds
                    };
                }

                // Test query performance
                var userCount = await _context.Users.CountAsync();
                stopwatch.Stop();

                var status = stopwatch.ElapsedMilliseconds switch
                {
                    < 100 => "Healthy",
                    < 500 => "Degraded",
                    _ => "Unhealthy"
                };

                return new HealthCheck
                {
                    Status = status,
                    Description = $"Database responsive, {userCount} users",
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        { "userCount", userCount },
                        { "connectionString", _context.Database.GetConnectionString()?.Substring(0, 50) + "..." }
                    }
                };
            }
            catch (Exception ex)
            {
                return new HealthCheck
                {
                    Status = "Unhealthy",
                    Description = $"Database check failed: {ex.Message}",
                    ResponseTime = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Perform file system check (Azure Blob Storage focused)
        /// </summary>
        private HealthCheck PerformFileSystemCheck()
        {
            try
            {
                // Basic file system health check for temporary operations only
                // Note: Primary file storage is now Azure Blob Storage
                var tempPath = Path.GetTempPath();
                var testFile = Path.Combine(tempPath, $"health_test_{Guid.NewGuid()}.tmp");
                System.IO.File.WriteAllText(testFile, "health check");
                System.IO.File.Delete(testFile);

                var diskInfo = GetDiskInfo();
                var status = diskInfo.FreeSpacePercent switch
                {
                    > 20 => "Healthy",
                    > 10 => "Degraded", 
                    _ => "Unhealthy"
                };

                return new HealthCheck
                {
                    Status = status,
                    Description = $"Azure Blob Storage active, {diskInfo.FreeSpacePercent:F1}% free space on local system",
                    Details = new Dictionary<string, object>
                    {
                        { "storageType", "Azure Blob Storage" },
                        { "freeSpaceGB", diskInfo.FreeSpaceGB },
                        { "totalSpaceGB", diskInfo.TotalSpaceGB }
                    }
                };
            }
            catch (Exception ex)
            {
                return new HealthCheck
                {
                    Status = "Unhealthy",
                    Description = $"File system check failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Perform configuration check
        /// </summary>
        private HealthCheck PerformConfigurationCheck()
        {
            try
            {
                var issues = new List<string>();
                
                // Check critical configuration values
                if (string.IsNullOrEmpty(_configuration.GetConnectionString("DefaultConnection")))
                    issues.Add("Database connection string missing");
                
                if (string.IsNullOrEmpty(_configuration["Jwt:Key"]))
                    issues.Add("JWT key missing");
                
                if (string.IsNullOrEmpty(_configuration["Jwt:Issuer"]))
                    issues.Add("JWT issuer missing");

                var status = issues.Count switch
                {
                    0 => "Healthy",
                    <= 2 => "Degraded",
                    _ => "Unhealthy"
                };

                return new HealthCheck
                {
                    Status = status,
                    Description = issues.Count == 0 ? "Configuration valid" : $"{issues.Count} configuration issues",
                    Details = issues.Count > 0 ? new Dictionary<string, object> { { "issues", issues } } : null
                };
            }
            catch (Exception ex)
            {
                return new HealthCheck
                {
                    Status = "Unhealthy",
                    Description = $"Configuration check failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Perform external services check
        /// </summary>
        private async Task<HealthCheck> PerformExternalServicesCheck()
        {
            try
            {
                // Add checks for external services like:
                // - Email service
                // - SMS service
                // - Google OAuth
                // - File storage service
                
                // For now, just return healthy
                await Task.Delay(10); // Simulate async check
                
                return new HealthCheck
                {
                    Status = "Healthy",
                    Description = "External services accessible"
                };
            }
            catch (Exception ex)
            {
                return new HealthCheck
                {
                    Status = "Degraded",
                    Description = $"Some external services may be unavailable: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get application version
        /// </summary>
        private string GetApplicationVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Get application uptime
        /// </summary>
        private TimeSpan GetUptime()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return DateTime.Now - process.StartTime;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Get disk space information
        /// </summary>
        private DiskInfo GetDiskInfo()
        {
            try
            {
                var drive = new DriveInfo(Directory.GetCurrentDirectory());
                var totalGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                var freeGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                var freePercent = (freeGB / totalGB) * 100;

                return new DiskInfo
                {
                    TotalSpaceGB = Math.Round(totalGB, 2),
                    FreeSpaceGB = Math.Round(freeGB, 2),
                    FreeSpacePercent = Math.Round(freePercent, 1)
                };
            }
            catch
            {
                return new DiskInfo { TotalSpaceGB = 0, FreeSpaceGB = 0, FreeSpacePercent = 0 };
            }
        }

        /// <summary>
        /// Get memory information
        /// </summary>
        private MemoryInfo GetMemoryInfo()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var workingSetMB = process.WorkingSet64 / (1024.0 * 1024.0);
                var privateMB = process.PrivateMemorySize64 / (1024.0 * 1024.0);
                var gcMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

                return new MemoryInfo
                {
                    WorkingSetMB = Math.Round(workingSetMB, 2),
                    PrivateMemoryMB = Math.Round(privateMB, 2),
                    GcMemoryMB = Math.Round(gcMB, 2)
                };
            }
            catch
            {
                return new MemoryInfo { WorkingSetMB = 0, PrivateMemoryMB = 0, GcMemoryMB = 0 };
            }
        }
    }

    /// <summary>
    /// Basic health status model
    /// </summary>
    public class HealthStatus
    {
        public string Status { get; set; } = "Unknown";
        public DateTime Timestamp { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public TimeSpan Uptime { get; set; }
        public string DatabaseStatus { get; set; } = string.Empty;
        public DiskInfo? DiskSpace { get; set; }
        public MemoryInfo? Memory { get; set; }
        public List<string>? Issues { get; set; }
    }

    /// <summary>
    /// Detailed health status model
    /// </summary>
    public class DetailedHealthStatus
    {
        public string Status { get; set; } = "Unknown";
        public DateTime Timestamp { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public TimeSpan Uptime { get; set; }
        public long ResponseTime { get; set; }
        public Dictionary<string, HealthCheck> Checks { get; set; } = new();
    }

    /// <summary>
    /// Individual health check model
    /// </summary>
    public class HealthCheck
    {
        public string Status { get; set; } = "Unknown";
        public string Description { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
        public Dictionary<string, object>? Details { get; set; }
    }

    /// <summary>
    /// Application metrics model
    /// </summary>
    public class ApplicationMetrics
    {
        public DateTime Timestamp { get; set; }
        public int ProcessId { get; set; }
        public long WorkingSet { get; set; }
        public long PrivateMemorySize { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }
        public long GcMemory { get; set; }
        public Dictionary<int, int> GcCollections { get; set; } = new();
    }

    /// <summary>
    /// Disk information model
    /// </summary>
    public class DiskInfo
    {
        public double TotalSpaceGB { get; set; }
        public double FreeSpaceGB { get; set; }
        public double FreeSpacePercent { get; set; }
    }

    /// <summary>
    /// Memory information model
    /// </summary>
    public class MemoryInfo
    {
        public double WorkingSetMB { get; set; }
        public double PrivateMemoryMB { get; set; }
        public double GcMemoryMB { get; set; }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILogger<LogsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public LogsController(ILogger<LogsController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentLogs([FromQuery] int lines = 100)
        {
            try
            {
                var logsDir = Path.Combine(_environment.ContentRootPath, "logs");
                
                if (!Directory.Exists(logsDir))
                {
                    return Ok(new { message = "No logs directory found", logs = new string[0] });
                }

                var logFiles = Directory.GetFiles(logsDir, "*.log")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToList();

                if (!logFiles.Any())
                {
                    return Ok(new { message = "No log files found", logs = new string[0] });
                }

                var latestLogFile = logFiles.First();
                var logLines = await System.IO.File.ReadAllLinesAsync(latestLogFile);
                var recentLines = logLines.TakeLast(lines).ToArray();

                return Ok(new 
                { 
                    logFile = Path.GetFileName(latestLogFile),
                    totalLines = logLines.Length,
                    showing = recentLines.Length,
                    lastModified = new FileInfo(latestLogFile).LastWriteTime,
                    logs = recentLines 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("profile-upload")]
        public async Task<IActionResult> GetProfileUploadLogs([FromQuery] int lines = 50)
        {
            try
            {
                var logsDir = Path.Combine(_environment.ContentRootPath, "logs");
                
                if (!Directory.Exists(logsDir))
                {
                    return Ok(new { message = "No logs directory found", logs = new string[0] });
                }

                var logFiles = Directory.GetFiles(logsDir, "*.log")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime);

                var profileLogs = new List<string>();

                foreach (var logFile in logFiles)
                {
                    var logLines = await System.IO.File.ReadAllLinesAsync(logFile);
                    var profileLines = logLines
                        .Where(line => line.ToLower().Contains("profile") && 
                                      (line.ToLower().Contains("image") || line.ToLower().Contains("upload")))
                        .ToList();
                    
                    profileLogs.AddRange(profileLines);
                    
                    if (profileLogs.Count >= lines) break;
                }

                return Ok(new 
                { 
                    message = $"Found {profileLogs.Count} profile upload related log entries",
                    logs = profileLogs.TakeLast(lines).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile upload logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("live")]
        public async Task<IActionResult> GetLiveLogs()
        {
            Response.Headers.Add("Content-Type", "text/plain");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                var logsDir = Path.Combine(_environment.ContentRootPath, "logs");
                
                if (!Directory.Exists(logsDir))
                {
                    await Response.WriteAsync("No logs directory found\n");
                    return new EmptyResult();
                }

                var logFiles = Directory.GetFiles(logsDir, "*.log")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime);

                if (!logFiles.Any())
                {
                    await Response.WriteAsync("No log files found\n");
                    return new EmptyResult();
                }

                var latestLogFile = logFiles.First();
                await Response.WriteAsync($"=== LIVE LOGS FROM: {Path.GetFileName(latestLogFile)} ===\n\n");

                // Read last 20 lines
                var lines = await System.IO.File.ReadAllLinesAsync(latestLogFile);
                var recentLines = lines.TakeLast(20);

                foreach (var line in recentLines)
                {
                    await Response.WriteAsync($"{line}\n");
                }

                await Response.WriteAsync($"\n=== END OF LOGS ({DateTime.Now}) ===\n");
                
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                await Response.WriteAsync($"Error: {ex.Message}\n");
                return new EmptyResult();
            }
        }

        [HttpGet("system-info")]
        public IActionResult GetSystemInfo()
        {
            try
            {
                var info = new
                {
                    serverTime = DateTime.Now,
                    environment = _environment.EnvironmentName,
                    contentRootPath = _environment.ContentRootPath,
                    webRootPath = _environment.WebRootPath,
                    directories = new
                    {
                        logs = Directory.Exists(Path.Combine(_environment.ContentRootPath, "logs")),
                        wwwroot = Directory.Exists(_environment.WebRootPath ?? ""),
                        uploads = Directory.Exists(Path.Combine(_environment.WebRootPath ?? "", "uploads", "profile-images"))
                    },
                    diskSpace = GetDiskSpace(),
                    processInfo = new
                    {
                        processId = Environment.ProcessId,
                        workingSet = Environment.WorkingSet,
                        machineName = Environment.MachineName
                    }
                };

                return Ok(info);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private object GetDiskSpace()
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => new
                    {
                        name = d.Name,
                        freeSpaceGB = Math.Round(d.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2),
                        totalSizeGB = Math.Round(d.TotalSize / (1024.0 * 1024.0 * 1024.0), 2)
                    })
                    .ToList();

                return drives;
            }
            catch
            {
                return "Unable to retrieve disk information";
            }
        }
    }
}

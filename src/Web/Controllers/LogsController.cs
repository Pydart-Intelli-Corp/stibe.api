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
                
                // Read file with shared access to allow Serilog to continue writing
                List<string> logLines = new List<string>();
                using (var fileStream = new FileStream(latestLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        logLines.Add(line);
                    }
                }
                
                var recentLines = logLines.TakeLast(lines).ToArray();

                return Ok(new 
                { 
                    logFile = Path.GetFileName(latestLogFile),
                    totalLines = logLines.Count,
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
                    // Read file with shared access
                    List<string> logLines = new List<string>();
                    using (var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            logLines.Add(line);
                        }
                    }
                    
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
            Response.Headers["Content-Type"] = "text/plain";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

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
                        storageType = "Azure Blob Storage"
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

        [HttpGet("push-operations")]
        public async Task<IActionResult> GetPushOperationLogs([FromQuery] int lines = 50)
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

                var pushLogs = new List<string>();

                foreach (var logFile in logFiles)
                {
                    // Read file with shared access
                    List<string> logLines = new List<string>();
                    using (var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            logLines.Add(line);
                        }
                    }
                    
                    var pushLines = logLines
                        .Where(line => line.Contains("📤 PUSH") || 
                                      (line.Contains("POST") || line.Contains("PUT") || line.Contains("PATCH")))
                        .ToList();
                    
                    pushLogs.AddRange(pushLines);
                    
                    if (pushLogs.Count >= lines) break;
                }

                return Ok(new 
                { 
                    message = $"Found {pushLogs.Count} push operation log entries",
                    logType = "push",
                    logs = pushLogs.TakeLast(lines).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving push operation logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("pull-operations")]
        public async Task<IActionResult> GetPullOperationLogs([FromQuery] int lines = 50)
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

                var pullLogs = new List<string>();

                foreach (var logFile in logFiles)
                {
                    // Read file with shared access
                    List<string> logLines = new List<string>();
                    using (var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            logLines.Add(line);
                        }
                    }
                    
                    var pullLines = logLines
                        .Where(line => line.Contains("📥 PULL") || line.Contains("GET"))
                        .ToList();
                    
                    pullLogs.AddRange(pullLines);
                    
                    if (pullLogs.Count >= lines) break;
                }

                return Ok(new 
                { 
                    message = $"Found {pullLogs.Count} pull operation log entries",
                    logType = "pull",
                    logs = pullLogs.TakeLast(lines).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pull operation logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("all-operations")]
        public async Task<IActionResult> GetAllOperationLogs([FromQuery] int lines = 100, [FromQuery] string? filter = null)
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

                var allLogs = new List<string>();

                foreach (var logFile in logFiles)
                {
                    // Read file with shared access
                    List<string> logLines = new List<string>();
                    using (var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            logLines.Add(line);
                        }
                    }
                    
                    var operationLines = logLines;

                    // Apply filter if provided
                    if (!string.IsNullOrEmpty(filter))
                    {
                        operationLines = logLines
                            .Where(line => line.ToLower().Contains(filter.ToLower()))
                            .ToList();
                    }
                    
                    allLogs.AddRange(operationLines);
                    
                    if (allLogs.Count >= lines) break;
                }

                // Count different operation types
                var pushCount = allLogs.Count(log => log.Contains("📤 PUSH"));
                var pullCount = allLogs.Count(log => log.Contains("📥 PULL"));
                var deleteCount = allLogs.Count(log => log.Contains("🗑️ DELETE"));
                var errorCount = allLogs.Count(log => log.Contains("❌") || log.Contains("ERROR"));

                return Ok(new 
                { 
                    message = $"Found {allLogs.Count} total log entries",
                    stats = new
                    {
                        total = allLogs.Count,
                        pushOperations = pushCount,
                        pullOperations = pullCount,
                        deleteOperations = deleteCount,
                        errors = errorCount
                    },
                    filter = filter,
                    logs = allLogs.TakeLast(lines).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all operation logs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("live-stream")]
        public async Task<IActionResult> GetLiveLogStream(CancellationToken cancellationToken)
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Access-Control-Allow-Origin"] = "*";

            try
            {
                var logsDir = Path.Combine(_environment.ContentRootPath, "logs");
                if (!Directory.Exists(logsDir))
                {
                    await Response.WriteAsync("event: error\n");
                    await Response.WriteAsync("data: No logs directory found\n\n");
                    await Response.Body.FlushAsync();
                    return new EmptyResult();
                }

                var logFiles = Directory.GetFiles(logsDir, "*.log")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .FirstOrDefault();

                if (logFiles == null)
                {
                    await Response.WriteAsync("event: error\n");
                    await Response.WriteAsync("data: No log files found\n\n");
                    await Response.Body.FlushAsync();
                    return new EmptyResult();
                }

                // Send initial connection success
                await Response.WriteAsync("event: connected\n");
                await Response.WriteAsync("data: Live log stream connected\n\n");
                await Response.Body.FlushAsync();

                // Keep track of file position
                long lastPosition = 0;
                var fileInfo = new FileInfo(logFiles);
                var lastHeartbeat = DateTime.UtcNow;
                
                // Start from end of file for live streaming
                lastPosition = fileInfo.Length;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        fileInfo.Refresh();
                        
                        if (fileInfo.Length > lastPosition)
                        {
                            using var stream = new FileStream(logFiles, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            stream.Seek(lastPosition, SeekOrigin.Begin);
                            
                            using var reader = new StreamReader(stream);
                            string? line;
                            
                            while ((line = await reader.ReadLineAsync()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    // Send each new log line as SSE event
                                    await Response.WriteAsync("event: log\n");
                                    await Response.WriteAsync($"data: {line}\n\n");
                                    await Response.Body.FlushAsync();
                                }
                            }
                            
                            lastPosition = fileInfo.Length;
                        }
                        
                        // Send heartbeat every 30 seconds
                        if (DateTime.UtcNow.Subtract(lastHeartbeat).TotalSeconds >= 30)
                        {
                            await Response.WriteAsync("event: heartbeat\n");
                            await Response.WriteAsync($"data: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n");
                            await Response.Body.FlushAsync();
                            lastHeartbeat = DateTime.UtcNow;
                        }
                        
                        // Wait for 2 seconds before checking for new logs
                        await Task.Delay(2000, cancellationToken);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        await Response.WriteAsync("event: error\n");
                        await Response.WriteAsync($"data: Error reading logs: {ex.Message}\n\n");
                        await Response.Body.FlushAsync();
                        await Task.Delay(5000, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected - this is normal, don't log as error
                _logger.LogInformation("Live log stream client disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in live log stream");
                try
                {
                    await Response.WriteAsync("event: error\n");
                    await Response.WriteAsync($"data: Stream error: {ex.Message}\n\n");
                    await Response.Body.FlushAsync();
                }
                catch
                {
                    // Response stream might be closed, ignore
                }
            }

            return new EmptyResult();
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

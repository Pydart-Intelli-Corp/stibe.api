// Services/Implementations/FileService/LocalFileService.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using stibe.api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace stibe.api.Services.Implementations.FileService
{
    public class LocalFileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalFileService> _logger;
        private readonly string _baseStoragePath;
        private readonly string _baseStorageUrl;
        // Modify the constructor in LocalFileService.cs
        public LocalFileService(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<LocalFileService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;

            // Fix: Use ContentRootPath instead of WebRootPath for reliable path construction
            var contentRootPath = _environment.ContentRootPath;
            _baseStoragePath = _configuration["FileStorage:LocalPath"]
                ?? Path.Combine(contentRootPath, "wwwroot", "uploads");

            // Base URL for accessing files (configure in appsettings.json)
            var configuredBaseUrl = _configuration["FileStorage:BaseUrl"] ?? "/uploads";
            
            // Handle both absolute and relative URLs
            if (configuredBaseUrl.StartsWith("http"))
            {
                _baseStorageUrl = configuredBaseUrl;
            }
            else
            {
                _baseStorageUrl = configuredBaseUrl;
            }

            _logger.LogInformation($"File storage path set to: {_baseStoragePath}");
            _logger.LogInformation($"File URL base set to: {_baseStorageUrl}");

            // Ensure base directory exists
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
                _logger.LogInformation($"Created directory: {_baseStoragePath}");
            }
        }


        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("UploadFileAsync: No file provided");
                return string.Empty;
            }

            try
            {
                _logger.LogInformation("=== LOCAL FILE SERVICE - UPLOAD STARTED ===");
                _logger.LogInformation("File: {FileName}, Size: {FileSize} bytes, Container: {ContainerName}", 
                    file.FileName, file.Length, containerName);
                _logger.LogInformation("Base storage path: {BasePath}", _baseStoragePath);
                _logger.LogInformation("Base storage URL: {BaseUrl}", _baseStorageUrl);

                // Create container directory if it doesn't exist
                var containerPath = Path.Combine(_baseStoragePath, containerName);
                _logger.LogInformation("Container path: {ContainerPath}", containerPath);
                
                if (!Directory.Exists(containerPath))
                {
                    _logger.LogInformation("Creating container directory: {ContainerPath}", containerPath);
                    Directory.CreateDirectory(containerPath);
                    _logger.LogInformation("Container directory created successfully");
                }
                else
                {
                    _logger.LogInformation("Container directory already exists");
                }

                // Create a unique filename
                var fileName = GetUniqueFileName(file.FileName);
                var filePath = Path.Combine(containerPath, fileName);
                
                _logger.LogInformation("Generated file name: {FileName}", fileName);
                _logger.LogInformation("Full file path: {FilePath}", filePath);

                // Save the file
                _logger.LogInformation("Starting file save operation...");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("File saved successfully");

                // Verify file was created
                if (!File.Exists(filePath))
                {
                    _logger.LogError("File verification failed: File not found at {FilePath}", filePath);
                    return string.Empty;
                }

                var fileInfo = new FileInfo(filePath);
                _logger.LogInformation("File verification successful - Size: {FileSize} bytes", fileInfo.Length);

                // Return the URL that can be used to access the file
                var resultUrl = $"{_baseStorageUrl}/{containerName}/{fileName}";
                _logger.LogInformation("Generated file URL: {FileUrl}", resultUrl);
                _logger.LogInformation("=== LOCAL FILE SERVICE - UPLOAD COMPLETED ===");
                
                return resultUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== LOCAL FILE SERVICE - ERROR === Exception during file upload: {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return string.Empty;
            }
        }

        public async Task<List<string>> UploadFilesAsync(IEnumerable<IFormFile> files, string containerName)
        {
            var urls = new List<string>();

            if (files == null || !files.Any())
            {
                return urls;
            }

            foreach (var file in files)
            {
                var url = await UploadFileAsync(file, containerName);
                if (!string.IsNullOrEmpty(url))
                {
                    urls.Add(url);
                }
            }

            return urls;
        }

        public Task DeleteFileAsync(string fileUrl, string containerName)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return Task.CompletedTask;
            }

            try
            {
                // Parse the URL to get the filename
                var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
                string fileName;

                if (uri.IsAbsoluteUri)
                {
                    fileName = Path.GetFileName(uri.LocalPath);
                }
                else
                {
                    var segments = uri.Segments.ToList();
                    fileName = segments.Last();
                }

                var filePath = Path.Combine(_baseStoragePath, containerName, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File {FilePath} deleted successfully", filePath);
                }
                else
                {
                    _logger.LogWarning("File {FilePath} not found for deletion", filePath);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileUrl} from container {ContainerName}",
                    fileUrl, containerName);
                return Task.CompletedTask;
            }
        }

        public async Task<string> UpdateProfileImageAsync(IFormFile newFile, string? oldFileUrl, string containerName)
        {
            if (newFile == null || newFile.Length == 0)
            {
                _logger.LogWarning("UpdateProfileImageAsync: No file provided");
                return string.Empty;
            }

            try
            {
                _logger.LogInformation("=== PROFILE IMAGE UPDATE STARTED ===");
                _logger.LogInformation("New file: {FileName}, Size: {FileSize} bytes, Container: {ContainerName}", 
                    newFile.FileName, newFile.Length, containerName);
                
                if (!string.IsNullOrEmpty(oldFileUrl))
                {
                    _logger.LogInformation("Deleting old profile image: {OldFileUrl}", oldFileUrl);
                    await DeleteFileAsync(oldFileUrl, containerName);
                }

                // Upload the new file
                var newFileUrl = await UploadFileAsync(newFile, containerName);
                
                _logger.LogInformation("Profile image updated successfully. New URL: {NewFileUrl}", newFileUrl);
                _logger.LogInformation("=== PROFILE IMAGE UPDATE COMPLETED ===");
                
                return newFileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== PROFILE IMAGE UPDATE ERROR === Exception during profile image update: {ErrorMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return string.Empty;
            }
        }

        private string GetUniqueFileName(string fileName)
        {
            // Generate a unique name by adding a timestamp and guid
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string uniqueId = Guid.NewGuid().ToString().Substring(0, 8);

            return $"{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
        }
    }
}

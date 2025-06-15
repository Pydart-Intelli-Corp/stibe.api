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
            _baseStorageUrl = _configuration["FileStorage:BaseUrl"] ?? "/uploads";

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
                return string.Empty;
            }

            try
            {
                // Create container directory if it doesn't exist
                var containerPath = Path.Combine(_baseStoragePath, containerName);
                if (!Directory.Exists(containerPath))
                {
                    Directory.CreateDirectory(containerPath);
                }

                // Create a unique filename
                var fileName = GetUniqueFileName(file.FileName);
                var filePath = Path.Combine(containerPath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return the URL that can be used to access the file
                return $"{_baseStorageUrl}/{containerName}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to container {ContainerName}",
                    file.FileName, containerName);
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

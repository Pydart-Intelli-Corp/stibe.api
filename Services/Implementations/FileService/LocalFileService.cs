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
                _logger.LogWarning("DeleteFileAsync: Empty file URL provided");
                return Task.CompletedTask;
            }

            try
            {
                _logger.LogInformation("=== DELETE FILE STARTED === URL: {FileUrl}, Container: {ContainerName}", fileUrl, containerName);
                
                // Parse the URL to get the filename
                string fileName;
                
                try
                {
                    var uri = new Uri(fileUrl, UriKind.RelativeOrAbsolute);
                    
                    if (uri.IsAbsoluteUri)
                    {
                        fileName = Path.GetFileName(uri.LocalPath);
                        _logger.LogInformation("Parsed absolute URI - FileName: {FileName}", fileName);
                    }
                    else
                    {
                        // Handle relative URLs like "/uploads/shop-images/filename.jpg"
                        var pathParts = fileUrl.Trim('/').Split('/').Where(p => !string.IsNullOrEmpty(p)).ToArray();
                        fileName = pathParts.LastOrDefault() ?? string.Empty;
                        _logger.LogInformation("Parsed relative URI - FileName: {FileName}", fileName);
                    }
                }
                catch (UriFormatException)
                {
                    // If URI parsing fails, try to extract filename from the end of the string
                    var pathParts = fileUrl.Replace("\\", "/").Split('/').Where(p => !string.IsNullOrEmpty(p)).ToArray();
                    fileName = pathParts.LastOrDefault() ?? string.Empty;
                    _logger.LogInformation("Fallback parsing - FileName: {FileName}", fileName);
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    _logger.LogWarning("Could not extract filename from URL: {FileUrl}", fileUrl);
                    return Task.CompletedTask;
                }

                var filePath = Path.Combine(_baseStoragePath, containerName, fileName);
                _logger.LogInformation("Attempting to delete file at path: {FilePath}", filePath);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("✅ File deleted successfully: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("⚠️ File not found for deletion: {FilePath}", filePath);
                    
                    // Try to find the file with different casing (case-insensitive search)
                    var directory = Path.GetDirectoryName(filePath);
                    if (Directory.Exists(directory))
                    {
                        var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
                        var matchingFile = files.FirstOrDefault(f => 
                            string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
                        
                        if (!string.IsNullOrEmpty(matchingFile))
                        {
                            File.Delete(matchingFile);
                            _logger.LogInformation("✅ File deleted with case-insensitive match: {FilePath}", matchingFile);
                        }
                        else
                        {
                            _logger.LogWarning("File not found even with case-insensitive search in directory: {Directory}", directory);
                        }
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting file {FileUrl} from container {ContainerName}: {ErrorMessage}",
                    fileUrl, containerName, ex.Message);
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

        public async Task DeleteMultipleFilesAsync(IEnumerable<string> fileUrls, string containerName)
        {
            if (fileUrls == null || !fileUrls.Any())
            {
                _logger.LogInformation("DeleteMultipleFilesAsync: No files to delete");
                return;
            }

            var fileUrlsList = fileUrls.ToList();
            
            try
            {
                _logger.LogInformation("=== BATCH FILE DELETION STARTED ===");
                _logger.LogInformation("Deleting {Count} files from container: {ContainerName}", fileUrlsList.Count, containerName);
                _logger.LogInformation("Files to delete: {FileUrls}", string.Join(", ", fileUrlsList));

                var deletedCount = 0;
                var failedCount = 0;
                
                // Delete files sequentially for better error tracking
                foreach (var fileUrl in fileUrlsList)
                {
                    try
                    {
                        await DeleteFileAsync(fileUrl, containerName);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogError(ex, "Failed to delete individual file: {FileUrl}", fileUrl);
                    }
                }
                
                _logger.LogInformation("=== BATCH FILE DELETION COMPLETED === Deleted: {DeletedCount}, Failed: {FailedCount}", 
                    deletedCount, failedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during batch file deletion in container {ContainerName}", containerName);
            }
        }

        public async Task<string> UpdateShopImageAsync(IFormFile newFile, string? oldFileUrl, string containerName, bool isProfileImage = false)
        {
            if (newFile == null || newFile.Length == 0)
            {
                _logger.LogWarning("UpdateShopImageAsync: No file provided");
                return string.Empty;
            }

            try
            {
                _logger.LogInformation("=== SHOP IMAGE UPDATE STARTED ===");
                _logger.LogInformation("New file: {FileName}, Size: {FileSize} bytes, Container: {ContainerName}, IsProfile: {IsProfile}", 
                    newFile.FileName, newFile.Length, containerName, isProfileImage);
                
                // Delete old image only if we're updating an existing image
                if (!string.IsNullOrEmpty(oldFileUrl))
                {
                    _logger.LogInformation("Deleting old shop image: {OldFileUrl}", oldFileUrl);
                    await DeleteFileAsync(oldFileUrl, containerName);
                }

                // Upload the new file
                var newFileUrl = await UploadFileAsync(newFile, containerName);
                
                _logger.LogInformation("Shop image updated successfully. New URL: {NewFileUrl}", newFileUrl);
                _logger.LogInformation("=== SHOP IMAGE UPDATE COMPLETED ===");
                
                return newFileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SHOP IMAGE UPDATE ERROR === Exception during shop image update: {ErrorMessage}", ex.Message);
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

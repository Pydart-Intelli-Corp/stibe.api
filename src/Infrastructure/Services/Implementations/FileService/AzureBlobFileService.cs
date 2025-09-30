// Services/Implementations/FileService/AzureBlobFileService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
    public class AzureBlobFileService : IFileService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureBlobFileService> _logger;
        private readonly string _containerName;
        private readonly string _baseUrl;
        private readonly string _containerSasToken;

        public AzureBlobFileService(
            IConfiguration configuration,
            ILogger<AzureBlobFileService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var connectionString = _configuration["FileStorage:Azure:ConnectionString"];
            _containerName = _configuration["FileStorage:Azure:ContainerName"] ?? "stibe-files";
            _baseUrl = _configuration["FileStorage:Azure:BaseUrl"] ?? "";
            
            // Container SAS token for accessing blobs - read from configuration
            _containerSasToken = _configuration["FileStorage:Azure:ContainerSasToken"] ?? "";

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Azure Storage connection string is not configured");
            }

            if (string.IsNullOrEmpty(_containerSasToken))
            {
                _logger.LogWarning("Container SAS token is not configured. Generated URLs may not be accessible.");
            }

            _blobServiceClient = new BlobServiceClient(connectionString);

            _logger.LogInformation($"Azure Blob Storage initialized with container: {_containerName}");
            _logger.LogInformation($"Azure Blob Storage base URL: {_baseUrl}");
            
            if (!string.IsNullOrEmpty(_containerSasToken))
            {
                _logger.LogInformation($"Using container SAS token (expires: 2027-01-01)");
            }
        }

        private async Task EnsureContainerExistsAsync()
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                // Create container without public access (private by default)
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
                _logger.LogInformation($"Azure container '{_containerName}' ensured to exist with private access");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to ensure container '{_containerName}' exists");
                throw;
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
                _logger.LogInformation("=== AZURE BLOB SERVICE - UPLOAD STARTED ===");
                _logger.LogInformation("File: {FileName}, Size: {FileSize} bytes, Container: {ContainerName}", 
                    file.FileName, file.Length, containerName);

                // Ensure container exists
                await EnsureContainerExistsAsync();

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Create a unique filename with container path
                var fileName = GetUniqueFileName(file.FileName);
                var blobName = $"{containerName}/{fileName}";
                
                _logger.LogInformation("Generated blob name: {BlobName}", blobName);

                var blobClient = containerClient.GetBlobClient(blobName);

                // Set content type based on file extension
                var contentType = GetContentType(file.FileName);

                // Upload options
                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "OriginalFileName", file.FileName },
                        { "UploadDate", DateTime.UtcNow.ToString("O") },
                        { "Container", containerName }
                    }
                };

                _logger.LogInformation("Starting blob upload...");
                await blobClient.UploadAsync(file.OpenReadStream(), uploadOptions);
                _logger.LogInformation("Blob uploaded successfully");

                // Generate URL using container SAS token
                var resultUrl = GenerateUrlWithContainerSas(blobName);

                _logger.LogInformation("Generated secure file URL with container SAS: {FileUrl}", resultUrl);
                _logger.LogInformation("=== AZURE BLOB SERVICE - UPLOAD COMPLETED ===");

                return resultUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== AZURE BLOB SERVICE - ERROR === Exception during file upload: {ErrorMessage}", ex.Message);
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

            _logger.LogInformation("=== BATCH UPLOAD STARTED === Uploading {Count} files", files.Count());

            var uploadTasks = files.Select(async file =>
            {
                var url = await UploadFileAsync(file, containerName);
                if (!string.IsNullOrEmpty(url))
                {
                    urls.Add(url);
                }
            });

            await Task.WhenAll(uploadTasks);

            _logger.LogInformation("=== BATCH UPLOAD COMPLETED === Successfully uploaded {Count} files", urls.Count);
            return urls;
        }

        public async Task DeleteFileAsync(string fileUrl, string containerName)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                _logger.LogWarning("DeleteFileAsync: No file URL provided");
                return;
            }

            try
            {
                _logger.LogInformation("=== AZURE BLOB DELETE STARTED ===");
                _logger.LogInformation("File URL to delete: {FileUrl}", fileUrl);
                _logger.LogInformation("Container context: {ContainerName}", containerName);

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                _logger.LogInformation("Using blob container client for: {MainContainer}", _containerName);
                
                // Extract blob name from URL
                var blobName = ExtractBlobNameFromUrl(fileUrl, containerName);
                
                if (string.IsNullOrEmpty(blobName))
                {
                    _logger.LogWarning("Could not extract blob name from URL: {FileUrl}", fileUrl);
                    return;
                }

                _logger.LogInformation("Final blob name for deletion: {BlobName}", blobName);

                var blobClient = containerClient.GetBlobClient(blobName);
                _logger.LogInformation("Created blob client for: {BlobName}", blobName);
                _logger.LogInformation("Blob client URI: {BlobUri}", blobClient.Uri);
                
                // Check if blob exists before trying to delete
                var existsResponse = await blobClient.ExistsAsync();
                _logger.LogInformation("Blob exists check: {Exists}", existsResponse.Value);
                
                if (!existsResponse.Value)
                {
                    _logger.LogWarning("Blob does not exist: {BlobName}", blobName);
                    return;
                }

                var response = await blobClient.DeleteIfExistsAsync();
                _logger.LogInformation("Delete operation result: {DeleteResult}", response.Value);

                if (response.Value)
                {
                    _logger.LogInformation("✅ Blob {BlobName} deleted successfully", blobName);
                }
                else
                {
                    _logger.LogWarning("❌ Blob {BlobName} was not deleted (may not exist)", blobName);
                }

                _logger.LogInformation("=== AZURE BLOB DELETE COMPLETED ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting blob from URL {FileUrl}: {ErrorMessage}", fileUrl, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            }
        }

        public async Task DeleteMultipleFilesAsync(IEnumerable<string> fileUrls, string containerName)
        {
            if (fileUrls == null || !fileUrls.Any())
            {
                return;
            }

            try
            {
                _logger.LogInformation("=== BATCH DELETE STARTED === Deleting {Count} files", fileUrls.Count());

                var deletionTasks = fileUrls.Select(fileUrl => DeleteFileAsync(fileUrl, containerName));
                await Task.WhenAll(deletionTasks);

                _logger.LogInformation("=== BATCH DELETE COMPLETED ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch file deletion");
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
                _logger.LogInformation("=== AZURE PROFILE IMAGE UPDATE STARTED ===");
                _logger.LogInformation("New file: {FileName}, Size: {FileSize} bytes", newFile.FileName, newFile.Length);
                _logger.LogInformation("Old file URL: {OldFileUrl}", oldFileUrl ?? "None");

                // Upload new image first
                var newFileUrl = await UploadFileAsync(newFile, containerName);
                
                if (string.IsNullOrEmpty(newFileUrl))
                {
                    _logger.LogError("Failed to upload new profile image");
                    return string.Empty;
                }

                // Delete old image only after successful upload
                if (!string.IsNullOrEmpty(oldFileUrl))
                {
                    _logger.LogInformation("Deleting old profile image: {OldFileUrl}", oldFileUrl);
                    try
                    {
                        await DeleteFileAsync(oldFileUrl, containerName);
                        _logger.LogInformation("Old profile image deleted successfully");
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete old profile image: {OldFileUrl}, but new image uploaded successfully", oldFileUrl);
                        // Don't fail the entire operation if old file deletion fails
                    }
                }

                _logger.LogInformation("Profile image updated successfully. New URL: {NewFileUrl}", newFileUrl);
                _logger.LogInformation("=== AZURE PROFILE IMAGE UPDATE COMPLETED ===");

                return newFileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== AZURE PROFILE IMAGE UPDATE ERROR === Exception: {ErrorMessage}", ex.Message);
                return string.Empty;
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
                _logger.LogInformation("=== AZURE SHOP IMAGE UPDATE STARTED ===");
                _logger.LogInformation("New file: {FileName}, Size: {FileSize} bytes, IsProfile: {IsProfile}", 
                    newFile.FileName, newFile.Length, isProfileImage);

                // Delete old image if exists
                if (!string.IsNullOrEmpty(oldFileUrl))
                {
                    _logger.LogInformation("Deleting old shop image: {OldFileUrl}", oldFileUrl);
                    await DeleteFileAsync(oldFileUrl, containerName);
                }

                // Upload new image
                var newFileUrl = await UploadFileAsync(newFile, containerName);

                _logger.LogInformation("Shop image updated successfully. New URL: {NewFileUrl}", newFileUrl);
                _logger.LogInformation("=== AZURE SHOP IMAGE UPDATE COMPLETED ===");

                return newFileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== AZURE SHOP IMAGE UPDATE ERROR === Exception: {ErrorMessage}", ex.Message);
                return string.Empty;
            }
        }

        private string GenerateUrlWithContainerSas(string blobName)
        {
            try
            {
                // Construct URL using the base URL and container SAS token
                var baseUri = $"https://stibestorage.blob.core.windows.net/{_containerName}";
                
                if (string.IsNullOrEmpty(_containerSasToken))
                {
                    _logger.LogWarning("No container SAS token configured, returning direct blob URL");
                    return $"{baseUri}/{blobName}";
                }
                
                var fullUrl = $"{baseUri}/{blobName}?{_containerSasToken}";
                
                _logger.LogInformation("Generated URL with container SAS token");
                return fullUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate URL with container SAS token");
                return string.Empty;
            }
        }

        private string GetUniqueFileName(string fileName)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string uniqueId = Guid.NewGuid().ToString().Substring(0, 8);

            return $"{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                _ => "application/octet-stream"
            };
        }

        private string ExtractBlobNameFromUrl(string fileUrl, string containerName)
        {
            try
            {
                _logger.LogInformation("=== BLOB NAME EXTRACTION DEBUG ===");
                _logger.LogInformation("Input URL: {FileUrl}", fileUrl);
                _logger.LogInformation("Container Name: {ContainerName}", containerName);
                _logger.LogInformation("Main Container: {MainContainer}", _containerName);
                
                // Remove SAS token parameters if present
                var urlWithoutQuery = fileUrl.Split('?')[0];
                _logger.LogInformation("URL without query: {CleanUrl}", urlWithoutQuery);
                
                var uri = new Uri(urlWithoutQuery);
                _logger.LogInformation("URI Host: {Host}", uri.Host);
                _logger.LogInformation("URI Path: {Path}", uri.AbsolutePath);
                
                // For Azure blob URLs, the path format is: /container-name/folder/file.ext
                var path = uri.AbsolutePath.TrimStart('/');
                _logger.LogInformation("Cleaned path: {CleanedPath}", path);
                
                // Example URL: https://stibestorage.blob.core.windows.net/stibe-datas/profile-images/file.jpg
                // Expected path: stibe-datas/profile-images/file.jpg
                // We need to return: profile-images/file.jpg (relative to container)
                
                string blobName;
                
                // Check if path starts with main container name
                if (path.StartsWith(_containerName + "/"))
                {
                    // Remove the main container name to get blob name relative to container
                    blobName = path.Substring(_containerName.Length + 1);
                    _logger.LogInformation("Removed main container prefix, blob name: {BlobName}", blobName);
                }
                else if (path.StartsWith(containerName + "/"))
                {
                    // Path starts with sub-container, it's already relative
                    blobName = path;
                    _logger.LogInformation("Path starts with sub-container, using: {BlobName}", blobName);
                }
                else if (path.Contains("/"))
                {
                    // Has path structure but doesn't start with expected container
                    blobName = path;
                    _logger.LogInformation("Path has structure, using as-is: {BlobName}", blobName);
                }
                else
                {
                    // Just a filename, construct full relative path
                    blobName = $"{containerName}/{path}";
                    _logger.LogInformation("Constructed path from filename: {BlobName}", blobName);
                }
                
                _logger.LogInformation("Final blob name for deletion: {BlobName}", blobName);
                return blobName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract blob name from URL: {FileUrl}", fileUrl);
                return string.Empty;
            }
        }
    }
}
// Services/Implementations/FileService/HybridFileService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using stibe.api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace stibe.api.Services.Implementations.FileService
{
    public class HybridFileService : IFileService
    {
        private readonly IFileService _activeService;
        private readonly LocalFileService _localService;
        private readonly AzureBlobFileService _azureService;
        private readonly ILogger<HybridFileService> _logger;
        private readonly string _currentProvider;

        public HybridFileService(
            LocalFileService localService,
            AzureBlobFileService azureService,
            IConfiguration configuration,
            ILogger<HybridFileService> logger)
        {
            _localService = localService;
            _azureService = azureService;
            _logger = logger;
            
            _currentProvider = configuration["FileStorage:Provider"]?.ToLowerInvariant() ?? "local";
            
            _activeService = _currentProvider switch
            {
                "azure" => _azureService,
                "local" => _localService,
                _ => _localService
            };

            _logger.LogInformation("=== HYBRID FILE SERVICE INITIALIZED ===");
            _logger.LogInformation("Active provider: {Provider}", _currentProvider.ToUpperInvariant());
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            try
            {
                _logger.LogInformation("Upload request routed to {Provider} service", _currentProvider.ToUpperInvariant());
                return await _activeService.UploadFileAsync(file, containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed with {Provider} service, attempting fallback", _currentProvider.ToUpperInvariant());
                
                // Fallback to local service if Azure fails
                if (_currentProvider == "azure" && _activeService != _localService)
                {
                    _logger.LogWarning("Falling back to local storage due to Azure failure");
                    return await _localService.UploadFileAsync(file, containerName);
                }
                
                throw;
            }
        }

        public async Task<List<string>> UploadFilesAsync(IEnumerable<IFormFile> files, string containerName)
        {
            try
            {
                _logger.LogInformation("Batch upload request routed to {Provider} service", _currentProvider.ToUpperInvariant());
                return await _activeService.UploadFilesAsync(files, containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch upload failed with {Provider} service, attempting fallback", _currentProvider.ToUpperInvariant());
                
                // Fallback to local service if Azure fails
                if (_currentProvider == "azure" && _activeService != _localService)
                {
                    _logger.LogWarning("Falling back to local storage for batch upload due to Azure failure");
                    return await _localService.UploadFilesAsync(files, containerName);
                }
                
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileUrl, string containerName)
        {
            try
            {
                await _activeService.DeleteFileAsync(fileUrl, containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete failed with {Provider} service", _currentProvider.ToUpperInvariant());
                
                // For delete operations, we might want to try both services to ensure cleanup
                if (_currentProvider == "azure")
                {
                    try
                    {
                        _logger.LogInformation("Attempting delete with local service as fallback");
                        await _localService.DeleteFileAsync(fileUrl, containerName);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback delete also failed");
                    }
                }
                
                throw;
            }
        }

        public async Task DeleteMultipleFilesAsync(IEnumerable<string> fileUrls, string containerName)
        {
            try
            {
                await _activeService.DeleteMultipleFilesAsync(fileUrls, containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch delete failed with {Provider} service", _currentProvider.ToUpperInvariant());
                
                // For delete operations, we might want to try both services to ensure cleanup
                if (_currentProvider == "azure")
                {
                    try
                    {
                        _logger.LogInformation("Attempting batch delete with local service as fallback");
                        await _localService.DeleteMultipleFilesAsync(fileUrls, containerName);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Fallback batch delete also failed");
                    }
                }
                
                throw;
            }
        }

        public async Task<string> UpdateProfileImageAsync(IFormFile newFile, string? oldFileUrl, string containerName)
        {
            try
            {
                return await _activeService.UpdateProfileImageAsync(newFile, oldFileUrl, containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile image update failed with {Provider} service, attempting fallback", _currentProvider.ToUpperInvariant());
                
                // Fallback to local service if Azure fails
                if (_currentProvider == "azure" && _activeService != _localService)
                {
                    _logger.LogWarning("Falling back to local storage for profile image update due to Azure failure");
                    return await _localService.UpdateProfileImageAsync(newFile, oldFileUrl, containerName);
                }
                
                throw;
            }
        }

        public async Task<string> UpdateShopImageAsync(IFormFile newFile, string? oldFileUrl, string containerName, bool isProfileImage = false)
        {
            try
            {
                return await _activeService.UpdateShopImageAsync(newFile, oldFileUrl, containerName, isProfileImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shop image update failed with {Provider} service, attempting fallback", _currentProvider.ToUpperInvariant());
                
                // Fallback to local service if Azure fails
                if (_currentProvider == "azure" && _activeService != _localService)
                {
                    _logger.LogWarning("Falling back to local storage for shop image update due to Azure failure");
                    return await _localService.UpdateShopImageAsync(newFile, oldFileUrl, containerName, isProfileImage);
                }
                
                throw;
            }
        }
    }
}

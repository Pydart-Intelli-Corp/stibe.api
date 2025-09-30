// Services/Interfaces/IFileService.cs
using Microsoft.AspNetCore.Http;

namespace stibe.api.Services.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string containerName);
        Task<List<string>> UploadFilesAsync(IEnumerable<IFormFile> files, string containerName);
        Task DeleteFileAsync(string fileUrl, string containerName);
        Task DeleteMultipleFilesAsync(IEnumerable<string> fileUrls, string containerName);
        Task<string> UpdateProfileImageAsync(IFormFile newFile, string? oldFileUrl, string containerName);
        Task<string> UpdateShopImageAsync(IFormFile newFile, string? oldFileUrl, string containerName, bool isProfileImage = false);
    }
}

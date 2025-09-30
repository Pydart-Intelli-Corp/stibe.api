using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace stibe.api.DTOs.FileUpload
{
    public class ProfileImageUploadDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }

    public class StaffImageUploadDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
        
        [Required]
        public int StaffId { get; set; }
    }

    public class ServiceImageUploadDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }

    public class ShopImageUploadDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
        
        [Required]
        public int ShopId { get; set; }
        
        public bool IsProfileImage { get; set; } = true;
    }
}
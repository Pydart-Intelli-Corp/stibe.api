using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs
{
    public class ShopStatusChangeRequestDto
    {
        [Required]
        public int ShopId { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ShopDefaultChangeRequestDto
    {
        [Required]
        public int ShopId { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ShopDeleteRequestDto
    {
        [Required]
        public int ShopId { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }
}

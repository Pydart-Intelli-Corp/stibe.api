using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs
{
    public class SalonStatusChangeRequestDto
    {
        [Required]
        public int SalonId { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class SalonDefaultChangeRequestDto
    {
        [Required]
        public int SalonId { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class SalonDeleteRequestDto
    {
        [Required]
        public int SalonId { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }
}

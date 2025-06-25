using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.Auth
{
    public class ProfileUpdateDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? ProfilePictureUrl { get; set; }
    }
}

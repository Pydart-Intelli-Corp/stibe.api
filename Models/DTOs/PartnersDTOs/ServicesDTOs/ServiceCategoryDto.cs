// Models/DTOs/PartnersDTOs/ServicesDTOs/ServiceCategoryDto.cs
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs
{
    public class ServiceCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ServiceCount { get; set; }
    }

    public class CreateServiceCategoryDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}

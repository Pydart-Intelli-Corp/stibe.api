using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity.ServicesEntity
{
    /// <summary>
    /// Entity for storing service name suggestions by category
    /// </summary>
    [Table("ServiceNameSuggestions")]
    public class ServiceNameSuggestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        public int Priority { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Entity for storing service description templates by category and service name
    /// </summary>
    [Table("ServiceDescriptionTemplates")]
    public class ServiceDescriptionTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ServiceName { get; set; } // NULL for category-wide templates

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int Priority { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
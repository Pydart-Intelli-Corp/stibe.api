using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs
{
    /// <summary>
    /// Request DTO for updating shop specializations
    /// </summary>
    public class UpdateShopSpecializationsRequestDto
    {
        [Required]
        public List<string> Specializations { get; set; } = new List<string>();
    }

    /// <summary>
    /// Response DTO for shop specialization statistics
    /// </summary>
    public class GetShopSpecializationStatsResponseDto
    {
        public List<ShopSpecializationStatsDto> SpecializationStats { get; set; } = new List<ShopSpecializationStatsDto>();
        public int TotalServices { get; set; }
        public int TotalActiveServices { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
    }

    /// <summary>
    /// Individual specialization statistics
    /// </summary>
    public class ShopSpecializationStatsDto
    {
        public string Specialization { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int ActiveServices { get; set; }
        public decimal AveragePrice { get; set; }
        public int TotalBookings { get; set; }
        public decimal Revenue { get; set; }
    }
}
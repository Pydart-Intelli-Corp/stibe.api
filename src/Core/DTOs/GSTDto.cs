using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs
{
    /// <summary>
    /// Detailed GST information from government database
    /// </summary>
    public class GSTDetailsDto
    {
        public string GSTNumber { get; set; } = string.Empty;
        public string TaxpayerName { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string PANNumber { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string BusinessNature { get; set; } = string.Empty;
        public string RegistrationDate { get; set; } = string.Empty;
        public string TaxpayerType { get; set; } = string.Empty;
        public string GSTStatus { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string CenterJurisdiction { get; set; } = string.Empty;
        public string StateJurisdiction { get; set; } = string.Empty;
        public List<string> BusinessActivities { get; set; } = new();
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// GST format validation result
    /// </summary>
    public class GSTValidationDto
    {
        public string GSTNumber { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public bool IsFormatValid { get; set; }
        public bool IsChecksumValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public GSTExtractedInfoDto? ExtractedInfo { get; set; }
    }

    /// <summary>
    /// Basic information extracted from GST number structure
    /// </summary>
    public class GSTExtractedInfoDto
    {
        public string GSTNumber { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string PANNumber { get; set; } = string.Empty;
        public string EntityNumber { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string CheckDigit { get; set; } = string.Empty;
        public bool IsValidFormat { get; set; }
    }

    /// <summary>
    /// GST search/lookup request
    /// </summary>
    public class GSTLookupRequestDto
    {
        [Required]
        [StringLength(15, MinimumLength = 15)]
        public string GSTNumber { get; set; } = string.Empty;
        
        public bool IncludeBusinessActivities { get; set; } = true;
        public bool IncludeContactInfo { get; set; } = false;
    }
}
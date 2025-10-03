using System.ComponentModel.DataAnnotations;

namespace stibe.api.src.Core.DTOs.ValidationDTOs
{
    public class GSTValidationRequest
    {
        [Required]
        [StringLength(15, MinimumLength = 15)]
        public string GstNumber { get; set; } = string.Empty;
    }

    public class GSTValidationResult
    {
        public bool IsValid { get; set; }
        public string GSTNumber { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? StateCode { get; set; }
        public string? StateName { get; set; }
        public string? PANNumber { get; set; }
        public string? EntityNumber { get; set; }
        public string? EntityType { get; set; }
        public string? RegistrationDate { get; set; }
        public string? Status { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public string DataSource { get; set; } = "API";
    }

    public class IFSCValidationRequest
    {
        [Required]
        [StringLength(11, MinimumLength = 11)]
        public string IfscCode { get; set; } = string.Empty;
    }

    public class IFSCValidationResult
    {
        public bool IsValid { get; set; }
        public string IFSCCode { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? BranchName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }
}
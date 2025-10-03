using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.Features
{
    public class AadhaarVerificationDto
    {
        [Required]
        [StringLength(12, MinimumLength = 12)]
        public string AadhaarNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;

        [Required]
        public string DocumentImageBase64 { get; set; } = string.Empty;
    }

    public class PanVerificationDto
    {
        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string PanNumber { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string DocumentImageBase64 { get; set; } = string.Empty;
    }

    public class DocumentOcrDto
    {
        [Required]
        public string DocumentImageBase64 { get; set; } = string.Empty;

        [Required]
        public string DocumentType { get; set; } = string.Empty;
    }

    public class FaceVerificationRequestDto
    {
        [Required]
        public string DocumentImageBase64 { get; set; } = string.Empty;

        [Required]
        public string LiveImageBase64 { get; set; } = string.Empty;
    }

    public class CompleteKycSubmissionDto
    {
        [Required]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required]
        public string FrontDocumentImageBase64 { get; set; } = string.Empty;

        public string? BackDocumentImageBase64 { get; set; }

        [Required]
        public string SelfieImageBase64 { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        public KycExtractedDataDto? ExtractedData { get; set; }
    }

    public class KycVerificationResponseDto
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public KycExtractedDataDto? ExtractedData { get; set; }
        public List<string>? ValidationErrors { get; set; }
        public string? TransactionId { get; set; }
        public float? ConfidenceScore { get; set; }
    }

    public class KycExtractedDataDto
    {
        public string? Name { get; set; }
        public string? DocumentNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? FatherName { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public float? ConfidenceScore { get; set; }
        public Dictionary<string, object>? AdditionalFields { get; set; }
    }

    public class FaceVerificationDto
    {
        public bool IsMatch { get; set; }
        public float ConfidenceScore { get; set; }
        public string? Message { get; set; }
    }

    public class KycStatusDto
    {
        public bool IsKycVerified { get; set; }
        public string KycStatus { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        public List<KycDocumentStatusDto> Documents { get; set; } = new();
    }

    public class KycDocumentStatusDto
    {
        public string DocumentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public float VerificationScore { get; set; }
    }

    public class KycApprovalDto
    {
        public string? AdminNotes { get; set; }
    }

    public class KycRejectionDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;

        public string? AdminNotes { get; set; }
    }
}
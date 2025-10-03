using stibe.api.Models.DTOs.Features;

namespace stibe.api.Services.Interfaces
{
    public interface IKycVerificationService
    {
        /// <summary>
        /// Verify Aadhaar number using government APIs or authorized third-party services
        /// </summary>
        Task<KycVerificationResponseDto> VerifyAadhaarAsync(string aadhaarNumber, string otp, string documentImageBase64);

        /// <summary>
        /// Verify PAN number using NSDL or other authorized APIs
        /// </summary>
        Task<KycVerificationResponseDto> VerifyPanAsync(string panNumber, string fullName, DateTime dateOfBirth, string documentImageBase64);

        /// <summary>
        /// Extract data from document image using OCR
        /// </summary>
        Task<KycExtractedDataDto> ExtractDocumentDataAsync(string documentImageBase64, string documentType);

        /// <summary>
        /// Verify face match between document photo and live photo
        /// </summary>
        Task<FaceVerificationDto> VerifyFaceMatchAsync(string documentImageBase64, string liveImageBase64);

        /// <summary>
        /// Send OTP to Aadhaar registered mobile number
        /// </summary>
        Task<bool> SendAadhaarOtpAsync(string aadhaarNumber);

        /// <summary>
        /// Validate document number format
        /// </summary>
        bool ValidateDocumentFormat(string documentNumber, string documentType);

        /// <summary>
        /// Get document validation rules for a specific type
        /// </summary>
        Task<DocumentValidationRulesDto> GetDocumentValidationRulesAsync(string documentType);

        /// <summary>
        /// Check if document number is blacklisted
        /// </summary>
        Task<bool> IsDocumentBlacklistedAsync(string documentNumber, string documentType);

        /// <summary>
        /// Generate verification report
        /// </summary>
        Task<KycVerificationReportDto> GenerateVerificationReportAsync(int userId);
    }

    public class DocumentValidationRulesDto
    {
        public string DocumentType { get; set; } = string.Empty;
        public string NumberPattern { get; set; } = string.Empty;
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public List<string> RequiredFields { get; set; } = new();
        public bool OcrRequired { get; set; }
        public bool FaceVerificationRequired { get; set; }
        public float MinConfidenceScore { get; set; } = 0.8f;
    }

    public class KycVerificationReportDto
    {
        public int UserId { get; set; }
        public string OverallStatus { get; set; } = string.Empty;
        public float OverallScore { get; set; }
        public List<DocumentVerificationDto> Documents { get; set; } = new();
        public List<VerificationAttemptDto> Attempts { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class DocumentVerificationDto
    {
        public string DocumentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public float Score { get; set; }
        public DateTime VerifiedAt { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    public class VerificationAttemptDto
    {
        public string AttemptType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public float? Score { get; set; }
        public DateTime AttemptedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
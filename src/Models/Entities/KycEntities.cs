using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using stibe.api.Models.Entities.PartnersEntity;

namespace stibe.api.Models.Entities
{
    [Table("KycVerifications")]
    public class KycVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DocumentNumber { get; set; }

        [StringLength(500)]
        public string? FrontImageUrl { get; set; }

        [StringLength(500)]
        public string? BackImageUrl { get; set; }

        [StringLength(500)]
        public string? SelfieImageUrl { get; set; }

        [Column(TypeName = "json")]
        public string? ExtractedData { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public float VerificationScore { get; set; }

        [StringLength(1000)]
        public string? RejectionReason { get; set; }

        [StringLength(1000)]
        public string? AdminNotes { get; set; }

        [Required]
        public DateTime SubmittedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public int? VerifiedBy { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("VerifiedBy")]
        public virtual User? VerifiedByUser { get; set; }
    }

    [Table("KycAuditLogs")]
    public class KycAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Details { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public int? AdminUserId { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("AdminUserId")]
        public virtual User? AdminUser { get; set; }
    }

    [Table("KycDocumentTemplates")]
    public class KycDocumentTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CountryCode { get; set; } = string.Empty;

        [Column(TypeName = "json")]
        public string ValidationRules { get; set; } = string.Empty;

        [Column(TypeName = "json")]
        public string OcrFieldMappings { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    [Table("KycVerificationAttempts")]
    public class KycVerificationAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DocumentNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string AttemptType { get; set; } = string.Empty; // OCR, Face_Verification, Document_Verification

        [Required]
        public bool Success { get; set; }

        public float? ConfidenceScore { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        [Column(TypeName = "json")]
        public string? ResponseData { get; set; }

        [Required]
        public DateTime AttemptedAt { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
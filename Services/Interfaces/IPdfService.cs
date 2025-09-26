using stibe.api.Models.DTOs;

namespace stibe.api.Services.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePaymentReceiptAsync(PaymentReceiptData receiptData);
        Task<string> GeneratePaymentReceiptFileAsync(PaymentReceiptData receiptData, string fileName);
    }

    public class PaymentReceiptData
    {
        public string PaymentId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal Savings { get; set; }
        public string Currency { get; set; } = "INR";
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public string Purpose { get; set; } = string.Empty;
        
        // GST Information
        public decimal BaseAmount { get; set; } // Amount before GST
        public decimal GstRate { get; set; } = 18.0m; // GST rate (18%)
        public decimal GstAmount { get; set; } // Calculated GST amount
        public string CompanyGstNumber { get; set; } = "32AAPCP4765K1ZW";
        public string? CustomerGstNumber { get; set; } // Customer's GST number if available
        
        // Customer Info
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        
        // Shop Info (if applicable)
        public string? ShopName { get; set; }
        public string? ShopAddress { get; set; }
        public string? ShopCity { get; set; }
        public string? ShopState { get; set; }
        public string? ShopZipCode { get; set; }
        
        // Coupon Info (if applicable)
        public string? CouponCode { get; set; }
        public string? CouponDescription { get; set; }
        public decimal DiscountPercentage { get; set; }
        
        // Company Info
        public string CompanyName { get; set; } = "Stibe";
        public string CompanyAddress { get; set; } = "Mumbai, Maharashtra, India";
        public string CompanyEmail { get; set; } = "info.pydart@gmail.com";
        public string CompanyPhone { get; set; } = "+91 9876543210";
        public string CompanyWebsite { get; set; } = "www.stibe.com";
    }
}

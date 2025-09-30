using stibe.api.Models.DTOs;

namespace stibe.api.Services.Interfaces
{
    public interface IGstService
    {
        /// <summary>
        /// Calculate GST for a given amount
        /// </summary>
        GstCalculation CalculateGst(decimal baseAmount, decimal gstRate = 18.0m);
        
        /// <summary>
        /// Calculate GST with coupon discount applied
        /// </summary>
        GstCalculation CalculateGstWithDiscount(decimal originalAmount, decimal discountAmount, decimal gstRate = 18.0m);
        
        /// <summary>
        /// Get GST breakdown for payment
        /// </summary>
        PaymentGstBreakdown GetPaymentGstBreakdown(decimal baseAmount, decimal? discountAmount = null, string? couponCode = null);
    }

    public class GstCalculation
    {
        public decimal BaseAmount { get; set; }
        public decimal GstRate { get; set; }
        public decimal GstAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string CompanyGstNumber { get; set; } = string.Empty;
    }

    public class PaymentGstBreakdown
    {
        public decimal OriginalAmount { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GstRate { get; set; }
        public decimal GstAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string? CouponCode { get; set; }
        public string CompanyGstNumber { get; set; } = string.Empty;
        public string? CustomerGstNumber { get; set; }
    }
}
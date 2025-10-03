using Microsoft.Extensions.Options;
using stibe.api.Configuration;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services.Implementations.General
{
    public class GstService : IGstService
    {
        private readonly PaymentSettings _paymentSettings;
        private readonly ILogger<GstService> _logger;

        public GstService(IOptions<PaymentSettings> paymentSettings, ILogger<GstService> logger)
        {
            _paymentSettings = paymentSettings.Value;
            _logger = logger;
        }

        public GstCalculation CalculateGst(decimal baseAmount, decimal gstRate = 18.0m)
        {
            try
            {
                var gstAmount = Math.Round((baseAmount * gstRate) / 100, 2, MidpointRounding.AwayFromZero);
                var totalAmount = baseAmount + gstAmount;

                _logger.LogInformation("GST Calculation: Base={BaseAmount}, Rate={GstRate}%, GST={GstAmount}, Total={TotalAmount}", 
                    baseAmount, gstRate, gstAmount, totalAmount);

                return new GstCalculation
                {
                    BaseAmount = baseAmount,
                    GstRate = gstRate,
                    GstAmount = gstAmount,
                    TotalAmount = totalAmount,
                    CompanyGstNumber = GetCompanyGstNumber()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating GST for amount: {BaseAmount}", baseAmount);
                throw;
            }
        }

        public GstCalculation CalculateGstWithDiscount(decimal originalAmount, decimal discountAmount, decimal gstRate = 18.0m)
        {
            try
            {
                var baseAmount = originalAmount - discountAmount;
                if (baseAmount < 0) baseAmount = 0;

                return CalculateGst(baseAmount, gstRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating GST with discount: Original={OriginalAmount}, Discount={DiscountAmount}", 
                    originalAmount, discountAmount);
                throw;
            }
        }

        public PaymentGstBreakdown GetPaymentGstBreakdown(decimal baseAmount, decimal? discountAmount = null, string? couponCode = null)
        {
            try
            {
                var gstRate = GetGstRate();
                
                if (discountAmount.HasValue && discountAmount.Value > 0)
                {
                    // When discount is provided, it represents the total discount from original total to final total
                    // We need to work with base amounts
                    
                    // Original amounts
                    var originalBaseAmount = baseAmount;
                    var originalGstAmount = originalBaseAmount * (gstRate / 100);
                    var originalTotalAmount = originalBaseAmount + originalGstAmount;
                    
                    // Final total after discount
                    var finalTotalAmount = originalTotalAmount - discountAmount.Value;
                    
                    // Extract final base amount from final total
                    var finalBaseAmount = finalTotalAmount / (1 + gstRate / 100);
                    var finalGstAmount = finalTotalAmount - finalBaseAmount;
                    
                    var breakdown = new PaymentGstBreakdown
                    {
                        OriginalAmount = originalBaseAmount, // Original base amount
                        BaseAmount = finalBaseAmount, // Final base amount after discount
                        DiscountAmount = discountAmount.Value, // Total discount amount
                        GstRate = gstRate,
                        GstAmount = finalGstAmount, // GST on final base amount
                        FinalAmount = finalTotalAmount, // Final total amount
                        CouponCode = couponCode,
                        CompanyGstNumber = GetCompanyGstNumber()
                    };

                    _logger.LogInformation("Payment GST Breakdown (Base-First Discount): OriginalBase={OriginalBase}, OriginalGST={OriginalGST}, OriginalTotal={OriginalTotal}, TotalDiscount={TotalDiscount}, FinalBase={FinalBase}, FinalGST={FinalGST}, FinalTotal={FinalTotal}", 
                        originalBaseAmount, originalGstAmount, originalTotalAmount, discountAmount.Value, finalBaseAmount, finalGstAmount, finalTotalAmount);

                    return breakdown;
                }
                else
                {
                    // No discount applied - standard GST calculation
                    var gstCalculation = CalculateGst(baseAmount, gstRate);

                    var breakdown = new PaymentGstBreakdown
                    {
                        OriginalAmount = baseAmount,
                        BaseAmount = baseAmount,
                        DiscountAmount = 0,
                        GstRate = gstRate,
                        GstAmount = gstCalculation.GstAmount,
                        FinalAmount = gstCalculation.TotalAmount,
                        CouponCode = couponCode,
                        CompanyGstNumber = GetCompanyGstNumber()
                    };

                    _logger.LogInformation("Payment GST Breakdown (No Discount): Base={BaseAmount}, GST={GstAmount}, Total={TotalAmount}", 
                        baseAmount, gstCalculation.GstAmount, gstCalculation.TotalAmount);

                    return breakdown;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment GST breakdown for amount: {BaseAmount}", baseAmount);
                throw;
            }
        }

        private decimal GetGstRate()
        {
            return _paymentSettings?.GST?.Rate ?? 18.0m;
        }

        private string GetCompanyGstNumber()
        {
            return _paymentSettings?.GST?.CompanyGSTNumber ?? "32AAPCP4765K1ZW";
        }
    }
}
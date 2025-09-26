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
                var discount = discountAmount ?? 0;
                var discountedBaseAmount = baseAmount - discount;
                
                if (discountedBaseAmount < 0) discountedBaseAmount = 0;

                var gstCalculation = CalculateGst(discountedBaseAmount, gstRate);

                var breakdown = new PaymentGstBreakdown
                {
                    OriginalAmount = baseAmount,
                    BaseAmount = discountedBaseAmount,
                    DiscountAmount = discount,
                    GstRate = gstRate,
                    GstAmount = gstCalculation.GstAmount,
                    FinalAmount = gstCalculation.TotalAmount,
                    CouponCode = couponCode,
                    CompanyGstNumber = GetCompanyGstNumber()
                };

                _logger.LogInformation("Payment GST Breakdown: Original={OriginalAmount}, Base={BaseAmount}, Discount={DiscountAmount}, GST={GstAmount}, Final={FinalAmount}", 
                    breakdown.OriginalAmount, breakdown.BaseAmount, breakdown.DiscountAmount, breakdown.GstAmount, breakdown.FinalAmount);

                return breakdown;
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
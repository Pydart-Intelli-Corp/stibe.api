using stibe.api.Models.DTOs;

namespace stibe.api.Services.Interfaces
{
    public interface ICouponService
    {
        Task<CouponValidationResponseDto> ValidateCouponAsync(ValidateCouponRequestDto request);
        Task<CouponApplicationResponseDto> ApplyCouponAsync(ApplyCouponRequestDto request);
        Task<bool> MarkCouponAsUsedAsync(string couponCode, int userId, string? paymentId = null);
        Task<List<CouponConfigDto>> GetAvailableCouponsAsync(string purpose = "SHOP_REGISTRATION");
        Task<CouponSystemConfigDto> GetCouponConfigAsync();
        Task<decimal> CalculateDiscountedAmountAsync(string couponCode, decimal originalAmount, string purpose);
    }
}
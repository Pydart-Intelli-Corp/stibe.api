namespace stibe.api.Configuration
{
    public class FeatureFlags
    {
        public bool UseRealEmailService { get; set; } = false;
        public bool UseRealSmsService { get; set; } = false;
        public bool UseRealPaymentService { get; set; } = false;
        public bool UseRealLocationService { get; set; } = false;
    }
}
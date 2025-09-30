namespace stibe.api.Configuration
{
    public class PaymentSettings
    {
        public RazorpaySettings Razorpay { get; set; } = new();
        public GstSettings GST { get; set; } = new();
    }

    public class RazorpaySettings
    {
        public string KeyId { get; set; } = string.Empty;
        public string KeySecret { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }

    public class GstSettings
    {
        public decimal Rate { get; set; } = 18.0m;
        public string CompanyGSTNumber { get; set; } = "32AAPCP4765K1ZW";
        public bool IncludedInPrice { get; set; } = false;
    }
}
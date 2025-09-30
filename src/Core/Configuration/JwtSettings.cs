namespace stibe.api.Configuration
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryInMinutes { get; set; } = 60;
        public int RefreshTokenExpiryInDays { get; set; } = 30;
        public bool EnableProactiveRefresh { get; set; } = true;
        public int ProactiveRefreshThresholdMinutes { get; set; } = 10;
    }
}
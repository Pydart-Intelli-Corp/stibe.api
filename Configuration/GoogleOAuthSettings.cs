namespace stibe.api.Configuration
{
    public class GoogleOAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string AndroidClientId { get; set; } = string.Empty;
        public List<string> SupportedClientTypes { get; set; } = new List<string> { "web" };
    }
}

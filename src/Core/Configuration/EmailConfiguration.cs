namespace stibe.api.Configuration
{
    public class EmailConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = "info.pydart@gmail.com"; // Default to username if not specified
        public string SenderName { get; set; } = "Stibe Booking";
        public bool EnableSSL { get; set; } = true;  // Match the casing in appsettings.json
    }
}

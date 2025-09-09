namespace stibe.api.Configuration
{
    public class PerformanceSettings
    {
        public bool EnableResponseCaching { get; set; } = false;
        public bool EnableCompression { get; set; } = true;
        public long MaxRequestBodySize { get; set; } = 10485760; // 10MB
        public int RequestTimeoutSeconds { get; set; } = 30;
        public int MaxConcurrentConnections { get; set; } = 100;
    }

    public class SecuritySettings
    {
        public bool EnableRateLimiting { get; set; } = true;
        public int MaxRequestsPerMinute { get; set; } = 100;
        public bool EnableApiKeyAuth { get; set; } = false;
        public bool RequireHttps { get; set; } = false;
        public List<string> AllowedHosts { get; set; } = new();
    }

    public class MonitoringSettings
    {
        public bool EnableHealthChecks { get; set; } = true;
        public bool EnableMetrics { get; set; } = true;
        public string AlertEmailAddress { get; set; } = string.Empty;
        public int HealthCheckIntervalSeconds { get; set; } = 30;
    }
}

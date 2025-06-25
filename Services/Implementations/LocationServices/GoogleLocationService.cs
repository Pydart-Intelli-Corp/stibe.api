using Microsoft.Extensions.Options;
using stibe.api.Configuration;
using stibe.api.Services.Interfaces;
using System.Text.Json;

namespace stibe.api.Services.Implementations.LocationServices
{
    public class GoogleLocationService : ILocationService
    {
        private readonly FeatureFlags _featureFlags;
        private readonly ILogger<GoogleLocationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GoogleLocationService(
            IOptions<FeatureFlags> featureFlags, 
            ILogger<GoogleLocationService> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _featureFlags = featureFlags.Value;
            _logger = logger;
            _httpClient = httpClient;
            _apiKey = configuration["GoogleMaps:ApiKey"] ?? "";
        }

        public async Task<(decimal? Latitude, decimal? Longitude)> GetCoordinatesAsync(string address, string city, string state, string zipCode)
        {
            if (!_featureFlags.UseRealLocationService)
            {
                _logger.LogInformation("Real location service is disabled, falling back to mock");
                // Fall back to mock behavior for now
                return await GetMockCoordinatesAsync(city);
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Google Maps API key not configured, falling back to mock");
                return await GetMockCoordinatesAsync(city);
            }

            try
            {
                _logger.LogInformation("=== GOOGLE LOCATION SERVICE ===");
                
                // Format the address
                var formattedAddress = FormatAddress(address, city, state, zipCode);
                _logger.LogInformation($"Geocoding request for: {formattedAddress}");

                // Build the Google Geocoding API URL
                var encodedAddress = Uri.EscapeDataString(formattedAddress);
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var geocodeResponse = JsonSerializer.Deserialize<GoogleGeocodeResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (geocodeResponse?.Status == "OK" && geocodeResponse.Results?.Length > 0)
                {
                    var location = geocodeResponse.Results[0].Geometry.Location;
                    var latitude = (decimal)location.Lat;
                    var longitude = (decimal)location.Lng;

                    _logger.LogInformation($"Google geocoding successful: {latitude}, {longitude}");
                    _logger.LogInformation("=== END GOOGLE LOCATION ===");

                    return (latitude, longitude);
                }
                else
                {
                    _logger.LogWarning($"Google geocoding failed: {geocodeResponse?.Status}");
                    _logger.LogInformation("=== END GOOGLE LOCATION ===");
                    
                    // Fall back to mock data
                    return await GetMockCoordinatesAsync(city);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Google Geocoding API");
                _logger.LogInformation("=== END GOOGLE LOCATION ===");
                
                // Fall back to mock data
                return await GetMockCoordinatesAsync(city);
            }
        }

        public double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double earthRadiusKm = 6371.0;

            var dLat = DegreesToRadians((double)(lat2 - lat1));
            var dLon = DegreesToRadians((double)(lon2 - lon1));

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians((double)lat1)) * Math.Cos(DegreesToRadians((double)lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        public async Task<bool> ValidateCoordinatesAsync(decimal latitude, decimal longitude)
        {
            await Task.Delay(50); // Simulate async operation
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }

        public string FormatAddress(string address, string city, string state, string zipCode)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(address)) parts.Add(address.Trim());
            if (!string.IsNullOrWhiteSpace(city)) parts.Add(city.Trim());
            if (!string.IsNullOrWhiteSpace(state)) parts.Add(state.Trim());
            if (!string.IsNullOrWhiteSpace(zipCode)) parts.Add(zipCode.Trim());

            return string.Join(", ", parts);
        }

        private async Task<(decimal? Latitude, decimal? Longitude)> GetMockCoordinatesAsync(string city)
        {
            _logger.LogInformation("Using fallback mock coordinates");
            
            // Mock coordinates for major cities (same as MockLocationService)
            var mockCoordinates = new Dictionary<string, (decimal Lat, decimal Lon)>
            {
                {"mumbai", (19.0760m, 72.8777m)},
                {"delhi", (28.7041m, 77.1025m)},
                {"bangalore", (12.9716m, 77.5946m)},
                {"kolkata", (22.5726m, 88.3639m)},
                {"chennai", (13.0827m, 80.2707m)},
                {"hyderabad", (17.3850m, 78.4867m)},
                {"pune", (18.5204m, 73.8567m)},
                {"ahmedabad", (23.0225m, 72.5714m)},
                {"kanayannur", (9.9312m, 76.2673m)},
                {"kochi", (9.9312m, 76.2673m)},
                {"kerala", (10.8505m, 76.2711m)}
            };

            await Task.Delay(200);

            var cityKey = city.ToLower().Trim();
            if (mockCoordinates.TryGetValue(cityKey, out var coordinates))
            {
                return (coordinates.Lat, coordinates.Lon);
            }

            // Default fallback
            return (9.9312m, 76.2673m);
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }

    // Google Geocoding API response models
    public class GoogleGeocodeResponse
    {
        public string Status { get; set; } = "";
        public GoogleGeocodeResult[]? Results { get; set; }
    }

    public class GoogleGeocodeResult
    {
        public GoogleGeometry Geometry { get; set; } = new();
    }

    public class GoogleGeometry
    {
        public GoogleLocation Location { get; set; } = new();
    }

    public class GoogleLocation
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}

using Microsoft.Extensions.Options;
using stibe.api.Configuration;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services.Implementations.MockServices
{
    public class MockLocationService : ILocationService
    {
        private readonly FeatureFlags _featureFlags;
        private readonly ILogger<MockLocationService> _logger;

        // Mock coordinates for major cities
        private readonly Dictionary<string, (decimal Lat, decimal Lon)> _mockCoordinates = new()
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

        public MockLocationService(IOptions<FeatureFlags> featureFlags, ILogger<MockLocationService> logger)
        {
            _featureFlags = featureFlags.Value;
            _logger = logger;
        }

        public async Task<(decimal? Latitude, decimal? Longitude)> GetCoordinatesAsync(string address, string city, string state, string zipCode)
        {
            if (_featureFlags.UseRealLocationService)
            {
                throw new NotImplementedException("Real location service not implemented yet");
            }

            _logger.LogInformation("=== MOCK LOCATION SERVICE ===");
            _logger.LogInformation($"Geocoding request for: {address}, {city}, {state} {zipCode}");

            await Task.Delay(200);

            var cityKey = city.ToLower().Trim();
            if (_mockCoordinates.TryGetValue(cityKey, out var coordinates))
            {
                var random = new Random();
                var latVariation = (decimal)(random.NextDouble() - 0.5) * 0.1m;
                var lonVariation = (decimal)(random.NextDouble() - 0.5) * 0.1m;

                var lat = coordinates.Lat + latVariation;
                var lon = coordinates.Lon + lonVariation;

                _logger.LogInformation($"Mock coordinates found: {lat}, {lon}");
                _logger.LogInformation("=== END MOCK LOCATION ===");

                return (lat, lon);
            }

            _logger.LogInformation("City not found in mock data, using default coordinates");
            _logger.LogInformation("=== END MOCK LOCATION ===");
            return (9.9312m, 76.2673m);
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
            await Task.Delay(50);
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

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
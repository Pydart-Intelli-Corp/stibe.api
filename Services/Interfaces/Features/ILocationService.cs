namespace stibe.api.Services.Interfaces
{
    public interface ILocationService
    {
        Task<(decimal? Latitude, decimal? Longitude)> GetCoordinatesAsync(string address, string city, string state, string zipCode);
        double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2);
        Task<bool> ValidateCoordinatesAsync(decimal latitude, decimal longitude);
        string FormatAddress(string address, string city, string state, string zipCode);
    }
}
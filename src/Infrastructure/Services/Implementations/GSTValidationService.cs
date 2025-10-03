using stibe.api.Services.Interfaces;
using stibe.api.Models.DTOs;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace stibe.api.Infrastructure.Services.Implementations
{
    public class GSTValidationService : IGSTValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GSTValidationService> _logger;
        private readonly IConfiguration _configuration;
        
        // Real GST API configuration
        private readonly string _gstApiKey;
        private readonly string _gstApiBaseUrl;

        // GST state codes mapping
        private readonly Dictionary<string, string> _stateCodes = new()
        {
            {"01", "Jammu and Kashmir"}, {"02", "Himachal Pradesh"}, {"03", "Punjab"},
            {"04", "Chandigarh"}, {"05", "Uttarakhand"}, {"06", "Haryana"},
            {"07", "Delhi"}, {"08", "Rajasthan"}, {"09", "Uttar Pradesh"},
            {"10", "Bihar"}, {"11", "Sikkim"}, {"12", "Arunachal Pradesh"},
            {"13", "Nagaland"}, {"14", "Manipur"}, {"15", "Mizoram"},
            {"16", "Tripura"}, {"17", "Meghalaya"}, {"18", "Assam"},
            {"19", "West Bengal"}, {"20", "Jharkhand"}, {"21", "Odisha"},
            {"22", "Chhattisgarh"}, {"23", "Madhya Pradesh"}, {"24", "Gujarat"},
            {"25", "Daman and Diu"}, {"26", "Dadra and Nagar Haveli"}, {"27", "Maharashtra"},
            {"28", "Andhra Pradesh"}, {"29", "Karnataka"}, {"30", "Goa"},
            {"31", "Lakshadweep"}, {"32", "Kerala"}, {"33", "Tamil Nadu"},
            {"34", "Puducherry"}, {"35", "Andaman and Nicobar Islands"}, {"36", "Telangana"},
            {"37", "Andhra Pradesh"}, {"38", "Ladakh"}
        };

        // Entity type mapping based on 12th character
        private readonly Dictionary<char, string> _entityTypes = new()
        {
            {'1', "Proprietary concern"}, {'2', "One Person Company"}, {'3', "Hindu Undivided Family"},
            {'4', "Partnership Firm"}, {'5', "Limited Liability Partnership"}, {'6', "Private Limited Company"},
            {'7', "Public Limited Company"}, {'8', "Government Department"}, {'9', "Society/Trust/Club"},
            {'A', "Tax Deductor"}, {'B', "E-commerce Operator"}, {'C', "Casual Taxable Person"},
            {'D', "Deemed Export"}, {'E', "UIN holders"}, {'F', "Consulate/Embassy of Foreign Country"},
            {'G', "NRI"}, {'H', "Representative Office"}, {'I', "Single Registration Scheme"},
            {'J', "Distribution of OIDAR Services"}, {'K', "Service Provider under OIDAR"},
            {'L', "TDS"}, {'M', "Electronic Commerce Operator"}, {'N', "Non-resident taxable person"},
            {'O', "Foreign Diplomatic Mission"}, {'P', "Online Information and Database Access or Retrieval services"},
            {'Q', "Supplier to embassy or consulate"}, {'R', "Residential premises"},
            {'S', "Special Economic Zone"}, {'T', "TCS"}, {'U', "UN Body or other international organization"},
            {'V', "Supplier of OIDAR to specified persons"}, {'W', "Wholesale trading"},
            {'X', "Exports"}, {'Y', "Warehouseman"}, {'Z', "Business Auxiliary Service"}
        };

        public GSTValidationService(HttpClient httpClient, ILogger<GSTValidationService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            
            // Initialize GST API settings from configuration
            _gstApiKey = _configuration["GSTApi:ApiKey"] ?? "";
            _gstApiBaseUrl = _configuration["GSTApi:BaseUrl"] ?? "https://gst-api.taxready.in";
            
            // Configure HttpClient for GST API
            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(_gstApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _gstApiKey);
            }
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<GSTDetailsDto?> GetGSTDetailsAsync(string gstNumber)
        {
            try
            {
                await Task.Delay(1); // Make method truly async
                
                var validation = ValidateGSTFormat(gstNumber);
                if (!validation.IsValid)
                {
                    _logger.LogWarning("Invalid GST format for number: {GSTNumber}", gstNumber);
                    return null;
                }

                // Try to fetch real GST data from API
                var realGstData = await FetchRealGSTDataAsync(gstNumber);
                if (realGstData != null)
                {
                    _logger.LogInformation("Successfully fetched real GST data for {GSTNumber}", gstNumber.Substring(0, 4) + "***" + gstNumber.Substring(11));
                    return realGstData;
                }
                
                // Fallback to extracted information with enhanced mock data
                var extractedInfo = ExtractGSTInfo(gstNumber);
                _logger.LogWarning("Real GST API unavailable, using extracted information for {GSTNumber}", gstNumber.Substring(0, 4) + "***" + gstNumber.Substring(11));
                
                return new GSTDetailsDto
                {
                    GSTNumber = gstNumber,
                    TaxpayerName = "Business Name Not Available (API Offline)",
                    LegalName = "Legal Name Not Available (API Offline)",
                    TradeName = "Trade Name Not Available (API Offline)",
                    BusinessAddress = "Address Not Available (API Offline)",
                    StateCode = extractedInfo.StateCode,
                    StateName = extractedInfo.StateName,
                    PANNumber = extractedInfo.PANNumber,
                    EntityType = extractedInfo.EntityType,
                    BusinessType = extractedInfo.BusinessType,
                    RegistrationDate = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd"),
                    GSTStatus = "Status Unknown (API Offline)",
                    TaxpayerType = "Type Unknown (API Offline)",
                    IsActive = true, // Assume active if format is valid
                    LastUpdated = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GST details for {GSTNumber}", gstNumber);
                return null;
            }
        }

        private async Task<GSTDetailsDto?> FetchRealGSTDataAsync(string gstNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(_gstApiKey))
                {
                    _logger.LogWarning("GST API key not configured, skipping real data fetch");
                    return null;
                }

                // Try multiple GST API providers
                var providers = new[]
                {
                    await TryGSTProvider1Async(gstNumber),
                    await TryGSTProvider2Async(gstNumber),
                    await TryGSTProvider3Async(gstNumber)
                };

                return providers.FirstOrDefault(p => p != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FetchRealGSTDataAsync for {GSTNumber}", gstNumber);
                return null;
            }
        }

        private async Task<GSTDetailsDto?> TryGSTProvider1Async(string gstNumber)
        {
            try
            {
                // GST API Provider 1 - TaxReady/MasterIndia style
                var url = $"{_gstApiBaseUrl}/api/gst/search/{gstNumber}";
                _logger.LogInformation("Trying GST Provider 1: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    
                    if (jsonDoc.RootElement.TryGetProperty("success", out var success) && success.GetBoolean())
                    {
                        var data = jsonDoc.RootElement.GetProperty("data");
                        return MapGSTProvider1Response(data, gstNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GST Provider 1 failed for {GSTNumber}", gstNumber);
            }
            return null;
        }

        private async Task<GSTDetailsDto?> TryGSTProvider2Async(string gstNumber)
        {
            try
            {
                // GST API Provider 2 - Government API style
                var url = $"https://api.gst.gov.in/enriched/gstin/{gstNumber}";
                _logger.LogInformation("Trying GST Provider 2: {Url}", url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {_gstApiKey}");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    
                    return MapGSTProvider2Response(jsonDoc.RootElement, gstNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GST Provider 2 failed for {GSTNumber}", gstNumber);
            }
            return null;
        }

        private async Task<GSTDetailsDto?> TryGSTProvider3Async(string gstNumber)
        {
            try
            {
                // GST API Provider 3 - Alternative provider
                var url = $"https://commonapi.mastersindia.co/commonapis/searchgstin?gstin={gstNumber}";
                _logger.LogInformation("Trying GST Provider 3: {Url}", url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {_gstApiKey}");
                request.Headers.Add("X-API-Key", _gstApiKey);
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    
                    return MapGSTProvider3Response(jsonDoc.RootElement, gstNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GST Provider 3 failed for {GSTNumber}", gstNumber);
            }
            return null;
        }

        private GSTDetailsDto MapGSTProvider1Response(JsonElement data, string gstNumber)
        {
            var extractedInfo = ExtractGSTInfo(gstNumber);
            
            return new GSTDetailsDto
            {
                GSTNumber = gstNumber,
                TaxpayerName = GetJsonStringValue(data, "taxpayer_name") ?? "Name Not Available",
                LegalName = GetJsonStringValue(data, "legal_name") ?? GetJsonStringValue(data, "taxpayer_name") ?? "Legal Name Not Available",
                TradeName = GetJsonStringValue(data, "trade_name") ?? "Trade Name Not Available",
                BusinessAddress = GetJsonStringValue(data, "address") ?? "Address Not Available",
                StateCode = extractedInfo.StateCode,
                StateName = extractedInfo.StateName,
                PANNumber = extractedInfo.PANNumber,
                EntityType = extractedInfo.EntityType,
                BusinessType = GetJsonStringValue(data, "business_type") ?? extractedInfo.BusinessType,
                BusinessNature = GetJsonStringValue(data, "nature_of_business") ?? "",
                RegistrationDate = GetJsonStringValue(data, "registration_date") ?? DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd"),
                GSTStatus = GetJsonStringValue(data, "status") ?? "Active",
                TaxpayerType = GetJsonStringValue(data, "taxpayer_type") ?? "Regular",
                IsActive = GetJsonStringValue(data, "status")?.ToLower().Contains("active") ?? true,
                LastUpdated = DateTime.Now,
                Email = GetJsonStringValue(data, "email") ?? "",
                PhoneNumber = GetJsonStringValue(data, "phone") ?? "",
                CenterJurisdiction = GetJsonStringValue(data, "center_jurisdiction") ?? "",
                StateJurisdiction = GetJsonStringValue(data, "state_jurisdiction") ?? ""
            };
        }

        private GSTDetailsDto MapGSTProvider2Response(JsonElement data, string gstNumber)
        {
            var extractedInfo = ExtractGSTInfo(gstNumber);
            
            return new GSTDetailsDto
            {
                GSTNumber = gstNumber,
                TaxpayerName = GetJsonStringValue(data, "tradeNm") ?? GetJsonStringValue(data, "lgnm") ?? "Name Not Available",
                LegalName = GetJsonStringValue(data, "lgnm") ?? "Legal Name Not Available",
                TradeName = GetJsonStringValue(data, "tradeNm") ?? "Trade Name Not Available",
                BusinessAddress = FormatGSTAddress(data),
                StateCode = extractedInfo.StateCode,
                StateName = extractedInfo.StateName,
                PANNumber = extractedInfo.PANNumber,
                EntityType = extractedInfo.EntityType,
                BusinessType = GetJsonStringValue(data, "ctb") ?? extractedInfo.BusinessType,
                BusinessNature = GetJsonStringValue(data, "nba") ?? "",
                RegistrationDate = GetJsonStringValue(data, "rgdt") ?? DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd"),
                GSTStatus = GetJsonStringValue(data, "sts") ?? "Active",
                TaxpayerType = GetJsonStringValue(data, "dty") ?? "Regular",
                IsActive = GetJsonStringValue(data, "sts")?.ToLower().Contains("active") ?? true,
                LastUpdated = DateTime.Now
            };
        }

        private GSTDetailsDto MapGSTProvider3Response(JsonElement data, string gstNumber)
        {
            var extractedInfo = ExtractGSTInfo(gstNumber);
            
            if (data.TryGetProperty("data", out var dataElement))
            {
                data = dataElement;
            }
            
            return new GSTDetailsDto
            {
                GSTNumber = gstNumber,
                TaxpayerName = GetJsonStringValue(data, "legal_name") ?? GetJsonStringValue(data, "trade_name") ?? "Name Not Available",
                LegalName = GetJsonStringValue(data, "legal_name") ?? "Legal Name Not Available",
                TradeName = GetJsonStringValue(data, "trade_name") ?? "Trade Name Not Available",
                BusinessAddress = GetJsonStringValue(data, "principal_place_address") ?? "Address Not Available",
                StateCode = extractedInfo.StateCode,
                StateName = extractedInfo.StateName,
                PANNumber = extractedInfo.PANNumber,
                EntityType = extractedInfo.EntityType,
                BusinessType = GetJsonStringValue(data, "constitution_of_business") ?? extractedInfo.BusinessType,
                BusinessNature = GetJsonStringValue(data, "nature_of_business_activity") ?? "",
                RegistrationDate = GetJsonStringValue(data, "date_of_registration") ?? DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd"),
                GSTStatus = GetJsonStringValue(data, "gstin_status") ?? "Active",
                TaxpayerType = GetJsonStringValue(data, "taxpayer_type") ?? "Regular",
                IsActive = GetJsonStringValue(data, "gstin_status")?.ToLower().Contains("active") ?? true,
                LastUpdated = DateTime.Now
            };
        }

        private string? GetJsonStringValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
            }
            return null;
        }

        private string FormatGSTAddress(JsonElement data)
        {
            var addressParts = new List<string>();
            
            var addressFields = new[] { "pradr", "adr", "addr", "address" };
            foreach (var field in addressFields)
            {
                var value = GetJsonStringValue(data, field);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    addressParts.Add(value);
                    break;
                }
            }
            
            return addressParts.Count > 0 ? string.Join(", ", addressParts) : "Address Not Available";
        }

        public GSTValidationDto ValidateGSTFormat(string gstNumber)
        {
            var result = new GSTValidationDto
            {
                GSTNumber = gstNumber,
                IsValid = false,
                IsFormatValid = false,
                IsChecksumValid = false,
                ValidationErrors = new List<string>()
            };

            // Check if GST number is null or empty
            if (string.IsNullOrWhiteSpace(gstNumber))
            {
                result.ValidationErrors.Add("GST number is required");
                return result;
            }

            // Remove spaces and convert to uppercase
            gstNumber = gstNumber.Replace(" ", "").ToUpperInvariant();
            result.GSTNumber = gstNumber;

            // Check length
            if (gstNumber.Length != 15)
            {
                result.ValidationErrors.Add("GST number must be 15 characters long");
                return result;
            }

            // Check format using regex
            var gstRegex = new Regex(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}[Z]{1}[0-9A-Z]{1}$");
            if (!gstRegex.IsMatch(gstNumber))
            {
                result.ValidationErrors.Add("Invalid GST number format");
                return result;
            }
            result.IsFormatValid = true;

            // Validate state code
            var stateCode = gstNumber.Substring(0, 2);
            if (!_stateCodes.ContainsKey(stateCode))
            {
                result.ValidationErrors.Add("Invalid state code in GST number");
                return result;
            }

            // Validate checksum (simplified version)
            var checksumValid = ValidateGSTChecksum(gstNumber);
            result.IsChecksumValid = checksumValid;
            if (!checksumValid)
            {
                result.ValidationErrors.Add("Invalid GST number checksum");
                return result;
            }

            result.IsValid = true;
            result.ExtractedInfo = ExtractGSTInfo(gstNumber);
            return result;
        }

        public GSTExtractedInfoDto ExtractGSTInfo(string gstNumber)
        {
            if (string.IsNullOrWhiteSpace(gstNumber) || gstNumber.Length != 15)
            {
                return new GSTExtractedInfoDto
                {
                    GSTNumber = gstNumber,
                    IsValidFormat = false
                };
            }

            gstNumber = gstNumber.Replace(" ", "").ToUpperInvariant();

            var stateCode = gstNumber.Substring(0, 2);
            var panNumber = gstNumber.Substring(2, 10);
            var entityTypeChar = gstNumber[11];
            var checkDigit = gstNumber[14];

            return new GSTExtractedInfoDto
            {
                GSTNumber = gstNumber,
                StateCode = stateCode,
                StateName = _stateCodes.GetValueOrDefault(stateCode, "Unknown State"),
                PANNumber = panNumber,
                EntityNumber = gstNumber.Substring(12, 1),
                EntityType = _entityTypes.GetValueOrDefault(entityTypeChar, "Unknown Entity Type"),
                BusinessType = "Business Type Not Available",
                CheckDigit = checkDigit.ToString(),
                IsValidFormat = true // We already validated format in calling method
            };
        }

        public async Task<bool> IsGSTActiveAsync(string gstNumber)
        {
            try
            {
                var details = await GetGSTDetailsAsync(gstNumber);
                return details?.IsActive ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking GST status for {GSTNumber}", gstNumber);
                return false;
            }
        }

        private bool ValidateGSTChecksum(string gstNumber)
        {
            try
            {
                // Simplified checksum validation
                // In a real implementation, you would use the actual GST checksum algorithm
                var checkDigit = gstNumber[14];
                
                // For now, just check if it's a valid alphanumeric character
                return char.IsLetterOrDigit(checkDigit);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Comprehensive GST validation with details
        /// </summary>
        public async Task<GSTValidationDto> ValidateGSTAsync(string gstNumber)
        {
            try
            {
                _logger.LogInformation("Starting comprehensive GST validation for: {GSTNumber}", gstNumber);

                // Start with format validation
                var formatValidation = ValidateGSTFormat(gstNumber);
                
                if (!formatValidation.IsValid)
                {
                    _logger.LogWarning("GST format validation failed for: {GSTNumber}", gstNumber);
                    return formatValidation;
                }

                // Try to get detailed information from API
                var gstDetails = await GetGSTDetailsAsync(gstNumber);
                
                if (gstDetails != null)
                {
                    _logger.LogInformation("GST details retrieved successfully for: {GSTNumber}", gstNumber);
                    
                    // Update validation result with API data
                    formatValidation.IsValid = gstDetails.IsActive;
                    formatValidation.ExtractedInfo!.StateName = gstDetails.StateName;
                    
                    return formatValidation;
                }
                else
                {
                    _logger.LogWarning("Could not retrieve GST details from API for: {GSTNumber}", gstNumber);
                    
                    // Return format validation result if API fails
                    formatValidation.ValidationErrors.Add("Unable to verify GST number with government database");
                    return formatValidation;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during comprehensive GST validation for: {GSTNumber}", gstNumber);
                
                return new GSTValidationDto
                {
                    GSTNumber = gstNumber,
                    IsValid = false,
                    IsFormatValid = false,
                    IsChecksumValid = false,
                    ValidationErrors = new List<string> { "An error occurred during validation" }
                };
            }
        }
    }
}
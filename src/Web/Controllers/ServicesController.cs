using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Services.Interfaces;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServicesController> _logger;
        private readonly IServiceSuggestionService _serviceSuggestionService;

        public ServicesController(
            ApplicationDbContext context,
            ILogger<ServicesController> logger,
            IServiceSuggestionService serviceSuggestionService)
        {
            _context = context;
            _logger = logger;
            _serviceSuggestionService = serviceSuggestionService;
        }

        /// <summary>
        /// Get service name suggestions for a specific category
        /// </summary>
        [HttpGet("suggestions/names/{category}")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetServiceNameSuggestions(string category)
        {
            try
            {
                _logger.LogInformation("Getting service name suggestions for category: {Category}", category);

                var suggestions = await _serviceSuggestionService.GetServiceNameSuggestionsAsync(category);

                if (suggestions == null || !suggestions.Any())
                {
                    _logger.LogWarning("No service name suggestions found for category: {Category}", category);
                    return Ok(ApiResponse<List<string>>.SuccessResponse(new List<string>(), "No suggestions found for the specified category"));
                }

                _logger.LogInformation("Retrieved {Count} service name suggestions for category: {Category}", suggestions.Count, category);
                return Ok(ApiResponse<List<string>>.SuccessResponse(suggestions, "Service name suggestions retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service name suggestions for category: {Category}", category);
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("An error occurred while retrieving service name suggestions"));
            }
        }

        /// <summary>
        /// Get service description suggestions for a specific category and service name
        /// </summary>
        [HttpGet("suggestions/descriptions/{category}/{serviceName}")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetServiceDescriptionSuggestions(string category, string serviceName)
        {
            try
            {
                _logger.LogInformation("Getting description suggestions for category: {Category}, service: {ServiceName}", category, serviceName);

                var suggestions = await _serviceSuggestionService.GetServiceDescriptionSuggestionsAsync(category, serviceName);

                if (suggestions == null || !suggestions.Any())
                {
                    _logger.LogWarning("No description suggestions found for category: {Category}, service: {ServiceName}", category, serviceName);
                    return Ok(ApiResponse<List<string>>.SuccessResponse(new List<string>(), "No suggestions found for the specified category and service"));
                }

                _logger.LogInformation("Retrieved {Count} description suggestions for category: {Category}, service: {ServiceName}", suggestions.Count, category, serviceName);
                return Ok(ApiResponse<List<string>>.SuccessResponse(suggestions, "Service description suggestions retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving description suggestions for category: {Category}, service: {ServiceName}", category, serviceName);
                return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("An error occurred while retrieving service description suggestions"));
            }
        }

        /// <summary>
        /// Get all available service categories with their service count
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<ServiceCategoryStatsDto>>>> GetServiceCategories()
        {
            try
            {
                _logger.LogInformation("Getting all service categories with statistics");

                var categories = await _serviceSuggestionService.GetServiceCategoriesAsync();

                _logger.LogInformation("Retrieved {Count} service categories", categories.Count);
                return Ok(ApiResponse<List<ServiceCategoryStatsDto>>.SuccessResponse(categories, "Service categories retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service categories");
                return StatusCode(500, ApiResponse<List<ServiceCategoryStatsDto>>.ErrorResponse("An error occurred while retrieving service categories"));
            }
        }

        /// <summary>
        /// Search service suggestions by keyword
        /// </summary>
        [HttpGet("suggestions/search")]
        public async Task<ActionResult<ApiResponse<ServiceSearchResultDto>>> SearchServiceSuggestions([FromQuery] string keyword, [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest(ApiResponse<ServiceSearchResultDto>.ErrorResponse("Search keyword is required"));
                }

                _logger.LogInformation("Searching service suggestions for keyword: {Keyword}", keyword);

                var searchResult = await _serviceSuggestionService.SearchServiceSuggestionsAsync(keyword, limit);

                _logger.LogInformation("Found {ServiceNamesCount} service names and {DescriptionsCount} descriptions for keyword: {Keyword}", 
                    searchResult.ServiceNames.Count, searchResult.Descriptions.Count, keyword);

                return Ok(ApiResponse<ServiceSearchResultDto>.SuccessResponse(searchResult, "Service suggestions search completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching service suggestions for keyword: {Keyword}", keyword);
                return StatusCode(500, ApiResponse<ServiceSearchResultDto>.ErrorResponse("An error occurred while searching service suggestions"));
            }
        }
    }

    /// <summary>
    /// DTO for service category statistics
    /// </summary>
    public class ServiceCategoryStatsDto
    {
        public string Category { get; set; } = string.Empty;
        public int ServiceNamesCount { get; set; }
        public int DescriptionsCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// DTO for service search results
    /// </summary>
    public class ServiceSearchResultDto
    {
        public List<string> ServiceNames { get; set; } = new();
        public List<string> Descriptions { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public int TotalResults { get; set; }
    }
}
using stibe.api.Controllers;

namespace stibe.api.Services.Interfaces
{
    /// <summary>
    /// Service for managing service name and description suggestions
    /// </summary>
    public interface IServiceSuggestionService
    {
        /// <summary>
        /// Get service name suggestions for a specific category
        /// </summary>
        Task<List<string>> GetServiceNameSuggestionsAsync(string category);

        /// <summary>
        /// Get service description suggestions for a specific category and service name
        /// </summary>
        Task<List<string>> GetServiceDescriptionSuggestionsAsync(string category, string serviceName);

        /// <summary>
        /// Get all available service categories with statistics
        /// </summary>
        Task<List<ServiceCategoryStatsDto>> GetServiceCategoriesAsync();

        /// <summary>
        /// Search service suggestions by keyword
        /// </summary>
        Task<ServiceSearchResultDto> SearchServiceSuggestionsAsync(string keyword, int limit = 20);

        /// <summary>
        /// Add new service name suggestion
        /// </summary>
        Task<bool> AddServiceNameSuggestionAsync(string category, string serviceName, int priority = 0);

        /// <summary>
        /// Add new service description suggestion
        /// </summary>
        Task<bool> AddServiceDescriptionSuggestionAsync(string category, string serviceName, string description, int priority = 0);

        /// <summary>
        /// Update suggestion priority
        /// </summary>
        Task<bool> UpdateSuggestionPriorityAsync(int suggestionId, int priority, string suggestionType);

        /// <summary>
        /// Deactivate suggestion
        /// </summary>
        Task<bool> DeactivateSuggestionAsync(int suggestionId, string suggestionType);
    }
}
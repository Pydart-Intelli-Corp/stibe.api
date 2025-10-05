using Microsoft.EntityFrameworkCore;
using stibe.api.Controllers;
using stibe.api.Data;
using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services.Implementations
{
    /// <summary>
    /// Implementation of service suggestion service
    /// </summary>
    public class ServiceSuggestionService : IServiceSuggestionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceSuggestionService> _logger;

        public ServiceSuggestionService(ApplicationDbContext context, ILogger<ServiceSuggestionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<string>> GetServiceNameSuggestionsAsync(string category)
        {
            try
            {
                _logger.LogInformation("Fetching service name suggestions for category: {Category}", category);

                // Check if we have suggestions in the database
                var dbSuggestions = await _context.ServiceNameSuggestions
                    .Where(s => s.Category.ToLower() == category.ToLower() && s.IsActive)
                    .OrderByDescending(s => s.Priority)
                    .ThenBy(s => s.ServiceName)
                    .Select(s => s.ServiceName)
                    .ToListAsync();

                if (dbSuggestions.Any())
                {
                    _logger.LogInformation("Found {Count} database suggestions for category: {Category}", dbSuggestions.Count, category);
                    return dbSuggestions;
                }

                // Fallback to hardcoded suggestions if no database suggestions exist
                var fallbackSuggestions = GetFallbackServiceNameSuggestions(category);
                
                // Optionally seed the database with fallback suggestions for future use
                if (fallbackSuggestions.Any())
                {
                    await SeedServiceNameSuggestionsAsync(category, fallbackSuggestions);
                }

                _logger.LogInformation("Using {Count} fallback suggestions for category: {Category}", fallbackSuggestions.Count, category);
                return fallbackSuggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching service name suggestions for category: {Category}", category);
                // Return fallback suggestions on error
                return GetFallbackServiceNameSuggestions(category);
            }
        }

        public async Task<List<string>> GetServiceDescriptionSuggestionsAsync(string category, string serviceName)
        {
            try
            {
                _logger.LogInformation("Fetching description suggestions for category: {Category}, service: {ServiceName}", category, serviceName);

                // Check database for specific service descriptions
                var specificDescriptions = await _context.ServiceDescriptionTemplates
                    .Where(t => t.Category.ToLower() == category.ToLower() && 
                               t.ServiceName != null && t.ServiceName.ToLower() == serviceName.ToLower() && 
                               t.IsActive)
                    .OrderByDescending(t => t.Priority)
                    .Select(t => t.Description)
                    .ToListAsync();

                if (specificDescriptions.Any())
                {
                    _logger.LogInformation("Found {Count} specific descriptions for {Category} - {ServiceName}", specificDescriptions.Count, category, serviceName);
                    return specificDescriptions;
                }

                // Check for category-wide descriptions
                var categoryDescriptions = await _context.ServiceDescriptionTemplates
                    .Where(t => t.Category.ToLower() == category.ToLower() && 
                               t.ServiceName == null && 
                               t.IsActive)
                    .OrderByDescending(t => t.Priority)
                    .Select(t => t.Description)
                    .Take(3) // Limit category-wide suggestions
                    .ToListAsync();

                if (categoryDescriptions.Any())
                {
                    // Generate personalized descriptions using category templates
                    var personalizedDescriptions = GeneratePersonalizedDescriptions(categoryDescriptions, serviceName);
                    _logger.LogInformation("Generated {Count} personalized descriptions using category templates", personalizedDescriptions.Count);
                    return personalizedDescriptions;
                }

                // Fallback to hardcoded suggestions
                var fallbackDescriptions = GetFallbackDescriptionSuggestions(category, serviceName);
                
                // Optionally seed the database
                if (fallbackDescriptions.Any())
                {
                    await SeedServiceDescriptionSuggestionsAsync(category, serviceName, fallbackDescriptions);
                }

                _logger.LogInformation("Using {Count} fallback descriptions for {Category} - {ServiceName}", fallbackDescriptions.Count, category, serviceName);
                return fallbackDescriptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching description suggestions for category: {Category}, service: {ServiceName}", category, serviceName);
                return GetFallbackDescriptionSuggestions(category, serviceName);
            }
        }

        public async Task<List<ServiceCategoryStatsDto>> GetServiceCategoriesAsync()
        {
            try
            {
                var categories = await _context.ServiceNameSuggestions
                    .Where(s => s.IsActive)
                    .GroupBy(s => s.Category)
                    .Select(g => new ServiceCategoryStatsDto
                    {
                        Category = g.Key,
                        ServiceNamesCount = g.Count(),
                        IsActive = true,
                        LastUpdated = g.Max(s => s.CreatedAt)
                    })
                    .ToListAsync();

                // Add description counts
                var descriptionCounts = await _context.ServiceDescriptionTemplates
                    .Where(t => t.IsActive)
                    .GroupBy(t => t.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var category in categories)
                {
                    var descCount = descriptionCounts.FirstOrDefault(d => d.Category == category.Category);
                    category.DescriptionsCount = descCount?.Count ?? 0;
                }

                return categories.OrderBy(c => c.Category).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching service categories");
                return new List<ServiceCategoryStatsDto>();
            }
        }

        public async Task<ServiceSearchResultDto> SearchServiceSuggestionsAsync(string keyword, int limit = 20)
        {
            try
            {
                var searchResult = new ServiceSearchResultDto();

                // Search service names
                var serviceNames = await _context.ServiceNameSuggestions
                    .Where(s => s.IsActive && s.ServiceName.ToLower().Contains(keyword.ToLower()))
                    .OrderByDescending(s => s.Priority)
                    .ThenBy(s => s.ServiceName)
                    .Select(s => s.ServiceName)
                    .Take(limit)
                    .ToListAsync();

                // Search descriptions
                var descriptions = await _context.ServiceDescriptionTemplates
                    .Where(t => t.IsActive && 
                               (t.Description.ToLower().Contains(keyword.ToLower()) || 
                                (t.ServiceName != null && t.ServiceName.ToLower().Contains(keyword.ToLower()))))
                    .OrderByDescending(t => t.Priority)
                    .Select(t => t.Description)
                    .Take(limit)
                    .ToListAsync();

                // Search categories
                var categories = await _context.ServiceNameSuggestions
                    .Where(s => s.IsActive && s.Category.ToLower().Contains(keyword.ToLower()))
                    .Select(s => s.Category)
                    .Distinct()
                    .ToListAsync();

                searchResult.ServiceNames = serviceNames;
                searchResult.Descriptions = descriptions;
                searchResult.Categories = categories;
                searchResult.TotalResults = serviceNames.Count + descriptions.Count + categories.Count;

                return searchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching service suggestions for keyword: {Keyword}", keyword);
                return new ServiceSearchResultDto();
            }
        }

        public async Task<bool> AddServiceNameSuggestionAsync(string category, string serviceName, int priority = 0)
        {
            try
            {
                var suggestion = new ServiceNameSuggestion
                {
                    Category = category,
                    ServiceName = serviceName,
                    Priority = priority,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ServiceNameSuggestions.Add(suggestion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added service name suggestion: {Category} - {ServiceName}", category, serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding service name suggestion: {Category} - {ServiceName}", category, serviceName);
                return false;
            }
        }

        public async Task<bool> AddServiceDescriptionSuggestionAsync(string category, string serviceName, string description, int priority = 0)
        {
            try
            {
                var suggestion = new ServiceDescriptionTemplate
                {
                    Category = category,
                    ServiceName = serviceName,
                    Description = description,
                    Priority = priority,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ServiceDescriptionTemplates.Add(suggestion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added service description suggestion: {Category} - {ServiceName}", category, serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding service description suggestion: {Category} - {ServiceName}", category, serviceName);
                return false;
            }
        }

        public async Task<bool> UpdateSuggestionPriorityAsync(int suggestionId, int priority, string suggestionType)
        {
            try
            {
                if (suggestionType.ToLower() == "name")
                {
                    var suggestion = await _context.ServiceNameSuggestions.FindAsync(suggestionId);
                    if (suggestion != null)
                    {
                        suggestion.Priority = priority;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                }
                else if (suggestionType.ToLower() == "description")
                {
                    var suggestion = await _context.ServiceDescriptionTemplates.FindAsync(suggestionId);
                    if (suggestion != null)
                    {
                        suggestion.Priority = priority;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating suggestion priority: {SuggestionId} - {Type}", suggestionId, suggestionType);
                return false;
            }
        }

        public async Task<bool> DeactivateSuggestionAsync(int suggestionId, string suggestionType)
        {
            try
            {
                if (suggestionType.ToLower() == "name")
                {
                    var suggestion = await _context.ServiceNameSuggestions.FindAsync(suggestionId);
                    if (suggestion != null)
                    {
                        suggestion.IsActive = false;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                }
                else if (suggestionType.ToLower() == "description")
                {
                    var suggestion = await _context.ServiceDescriptionTemplates.FindAsync(suggestionId);
                    if (suggestion != null)
                    {
                        suggestion.IsActive = false;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating suggestion: {SuggestionId} - {Type}", suggestionId, suggestionType);
                return false;
            }
        }

        #region Private Helper Methods

        private List<string> GetFallbackServiceNameSuggestions(string category)
        {
            var suggestions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Hair Styling"] = new List<string> { "Basic Haircut", "Hair Styling", "Blow Dry", "Hair Wash & Cut", "Kids Haircut", "Senior Haircut", "Layered Cut", "Bob Cut", "Pixie Cut", "Trim" },
                ["Hair Treatments"] = new List<string> { "Deep Conditioning", "Hair Mask", "Protein Treatment", "Keratin Treatment", "Hot Oil Treatment", "Scalp Treatment", "Hair Repair", "Anti-Dandruff Treatment" },
                ["Hair Coloring"] = new List<string> { "Full Color", "Root Touch-up", "Highlights", "Lowlights", "Balayage", "Ombre", "Color Correction", "Gray Coverage", "Fashion Colors", "Hair Glossing" },
                ["Skin Care"] = new List<string> { "Basic Facial", "Deep Cleansing", "Exfoliation", "Moisturizing Treatment", "Skin Analysis", "Blackhead Removal", "Pore Minimizing", "Skin Brightening" },
                ["Facial Treatments"] = new List<string> { "Anti-Aging Facial", "Hydrating Facial", "Acne Treatment", "Brightening Facial", "Vitamin C Facial", "Gold Facial", "Diamond Facial", "Oxygen Facial", "HydraFacial" },
                ["Makeup Services"] = new List<string> { "Bridal Makeup", "Party Makeup", "Professional Makeup", "Eye Makeup", "Makeup Consultation", "Makeup Trial", "Special Event Makeup", "Photoshoot Makeup" },
                ["Nail Care"] = new List<string> { "Basic Manicure", "Basic Pedicure", "Gel Manicure", "French Manicure", "Nail Filing", "Cuticle Care", "Hand Treatment", "Foot Treatment" },
                ["Nail Art & Design"] = new List<string> { "Nail Art", "Gel Polish", "Nail Extension", "Nail Design", "3D Nail Art", "Glitter Nails", "Ombre Nails", "Chrome Nails" },
                ["Massage Therapy"] = new List<string> { "Full Body Massage", "Back Massage", "Head Massage", "Foot Massage", "Hand Massage", "Deep Tissue Massage", "Swedish Massage", "Hot Stone Massage" },
                ["Body Treatments"] = new List<string> { "Body Scrub", "Body Wrap", "Mud Therapy", "Body Polishing", "Tan Removal", "Body Moisturizing", "Cellulite Treatment", "Body Detox" },
                ["Hair Removal"] = new List<string> { "Eyebrow Threading", "Upper Lip Threading", "Full Face Threading", "Body Waxing", "Brazilian Wax", "Bikini Wax", "Leg Wax", "Arm Wax", "Underarm Wax" },
                ["Eyebrow & Lash"] = new List<string> { "Eyebrow Shaping", "Eyebrow Tinting", "Eyelash Extension", "Eyelash Lift", "Eyelash Tinting", "Eyebrow Microblading", "Lash Perm" },
                ["Men's Grooming"] = new List<string> { "Men's Haircut", "Beard Trim", "Mustache Trim", "Head Shave", "Beard Styling", "Men's Facial", "Scalp Massage", "Hair Wash" },
                ["Bridal & Special Events"] = new List<string> { "Bridal Package", "Pre-Bridal Treatment", "Engagement Makeup", "Reception Makeup", "Mehendi Makeup", "Sangeet Makeup", "Party Package" },
                ["Wellness & Spa"] = new List<string> { "Spa Package", "Aromatherapy", "Steam Bath", "Sauna", "Jacuzzi", "Meditation Session", "Yoga Session", "Wellness Consultation" }
            };

            return suggestions.GetValueOrDefault(category, new List<string>());
        }

        private List<string> GetFallbackDescriptionSuggestions(string category, string serviceName)
        {
            var descriptions = new List<string>();

            // Generate category-specific descriptions
            switch (category.ToLower())
            {
                case "hair styling":
                    descriptions.Add($"Professional {serviceName.ToLower()} service tailored to your face shape and personal style. Our experienced stylists will consult with you to create the perfect look.");
                    descriptions.Add($"Transform your look with our precision {serviceName.ToLower()} service. We use the latest techniques to deliver a fresh, modern style that suits your lifestyle.");
                    descriptions.Add($"Expert {serviceName.ToLower()} designed to give you a polished, professional appearance. Includes consultation, wash, and basic styling.");
                    break;

                case "facial treatments":
                    descriptions.Add($"Luxurious {serviceName.ToLower()} treatment designed to rejuvenate and refresh your skin. Our expert aestheticians use premium products for optimal results.");
                    descriptions.Add($"Customized {serviceName.ToLower()} that addresses your specific skin concerns. Includes deep cleansing, exfoliation, and nourishing treatments.");
                    descriptions.Add($"Relaxing {serviceName.ToLower()} experience that leaves your skin glowing and revitalized. Perfect for all skin types and concerns.");
                    break;

                case "massage therapy":
                    descriptions.Add($"Therapeutic {serviceName.ToLower()} that relieves tension and promotes relaxation. Our skilled therapists use techniques to target your specific needs.");
                    descriptions.Add($"Rejuvenating {serviceName.ToLower()} designed to reduce stress and improve circulation. Experience ultimate relaxation in our serene environment.");
                    descriptions.Add($"Professional {serviceName.ToLower()} that combines relaxation with therapeutic benefits. Customized pressure and techniques for your comfort.");
                    break;

                default:
                    descriptions.Add($"Professional {serviceName.ToLower()} service delivered by our experienced team. We ensure the highest quality results tailored to your needs.");
                    descriptions.Add($"Premium {serviceName.ToLower()} experience designed to exceed your expectations. We use quality products and proven techniques.");
                    descriptions.Add($"Expert {serviceName.ToLower()} service in a comfortable, relaxing environment. Book your appointment today for the best results.");
                    break;
            }

            return descriptions;
        }

        private List<string> GeneratePersonalizedDescriptions(List<string> templates, string serviceName)
        {
            var personalizedDescriptions = new List<string>();

            foreach (var template in templates)
            {
                // Replace placeholders with service name
                var personalized = template
                    .Replace("{serviceName}", serviceName)
                    .Replace("{SERVICE_NAME}", serviceName.ToUpper())
                    .Replace("{service_name}", serviceName.ToLower());

                personalizedDescriptions.Add(personalized);
            }

            return personalizedDescriptions;
        }

        private async Task SeedServiceNameSuggestionsAsync(string category, List<string> suggestions)
        {
            try
            {
                var priority = 100;
                foreach (var suggestion in suggestions)
                {
                    await AddServiceNameSuggestionAsync(category, suggestion, priority--);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding service name suggestions for category: {Category}", category);
            }
        }

        private async Task SeedServiceDescriptionSuggestionsAsync(string category, string serviceName, List<string> descriptions)
        {
            try
            {
                var priority = 100;
                foreach (var description in descriptions)
                {
                    await AddServiceDescriptionSuggestionAsync(category, serviceName, description, priority--);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding service description suggestions for: {Category} - {ServiceName}", category, serviceName);
            }
        }

        #endregion
    }
}
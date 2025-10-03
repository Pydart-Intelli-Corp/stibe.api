using stibe.api.Models.DTOs;

namespace stibe.api.Services.Interfaces
{
    /// <summary>
    /// Service for GST number validation and information retrieval
    /// </summary>
    public interface IGSTValidationService
    {
        /// <summary>
        /// Get detailed GST information from government database
        /// </summary>
        /// <param name="gstNumber">15-digit GST number</param>
        /// <returns>Detailed GST information if found</returns>
        Task<GSTDetailsDto?> GetGSTDetailsAsync(string gstNumber);

        /// <summary>
        /// Validate GST number format and checksum
        /// </summary>
        /// <param name="gstNumber">GST number to validate</param>
        /// <returns>Validation result</returns>
        GSTValidationDto ValidateGSTFormat(string gstNumber);

        /// <summary>
        /// Extract basic information from GST number structure
        /// </summary>
        /// <param name="gstNumber">GST number to extract info from</param>
        /// <returns>Extracted information</returns>
        GSTExtractedInfoDto ExtractGSTInfo(string gstNumber);

        /// <summary>
        /// Check if GST number exists and is active
        /// </summary>
        /// <param name="gstNumber">GST number to check</param>
        /// <returns>True if GST number is active</returns>
        Task<bool> IsGSTActiveAsync(string gstNumber);

        /// <summary>
        /// Comprehensive GST validation with details
        /// </summary>
        /// <param name="gstNumber">GST number to validate</param>
        /// <returns>Complete validation result with details</returns>
        Task<GSTValidationDto> ValidateGSTAsync(string gstNumber);
    }
}
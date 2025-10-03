using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stibe.api.Models.DTOs;
using stibe.api.Models.DTOs.Features;
using stibe.api.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GSTController : ControllerBase
    {
        private readonly IGSTValidationService _gstValidationService;
        private readonly ILogger<GSTController> _logger;

        public GSTController(IGSTValidationService gstValidationService, ILogger<GSTController> logger)
        {
            _gstValidationService = gstValidationService;
            _logger = logger;
        }

        /// <summary>
        /// Get detailed GST information for a given GST number
        /// </summary>
        /// <param name="gstNumber">15-digit GST number</param>
        /// <returns>Detailed GST information including company name, address, etc.</returns>
        [HttpGet("details/{gstNumber}")]
        [AllowAnonymous] // Allow anonymous access for GST verification
        public async Task<ActionResult<ApiResponse<GSTDetailsDto>>> GetGSTDetails(
            [Required] [StringLength(15, MinimumLength = 15)] string gstNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gstNumber) || gstNumber.Length != 15)
                {
                    return BadRequest(ApiResponse<GSTDetailsDto>.ErrorResponse("Invalid GST number format. GST number must be exactly 15 characters."));
                }

                _logger.LogInformation("GST details lookup requested for: {GSTNumber}", gstNumber.Substring(0, 4) + "***" + gstNumber.Substring(11));

                var gstDetails = await _gstValidationService.GetGSTDetailsAsync(gstNumber.ToUpperInvariant());

                if (gstDetails == null)
                {
                    return NotFound(ApiResponse<GSTDetailsDto>.ErrorResponse("GST number not found or invalid."));
                }

                return Ok(ApiResponse<GSTDetailsDto>.SuccessResponse(gstDetails, "GST details retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<GSTDetailsDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving GST details for number: {GSTNumber}", gstNumber?.Substring(0, 4) + "***");
                return StatusCode(500, ApiResponse<GSTDetailsDto>.ErrorResponse("An error occurred while retrieving GST details"));
            }
        }

        /// <summary>
        /// Validate GST number format
        /// </summary>
        /// <param name="gstNumber">GST number to validate</param>
        /// <returns>Validation result with format check</returns>
        [HttpGet("validate/{gstNumber}")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<GSTValidationDto>> ValidateGSTFormat(
            [Required] string gstNumber)
        {
            try
            {
                var validation = _gstValidationService.ValidateGSTFormat(gstNumber);
                return Ok(ApiResponse<GSTValidationDto>.SuccessResponse(validation, "GST validation completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating GST number format");
                return StatusCode(500, ApiResponse<GSTValidationDto>.ErrorResponse("An error occurred while validating GST number"));
            }
        }

        /// <summary>
        /// Extract basic information from GST number without API lookup
        /// </summary>
        /// <param name="gstNumber">GST number to extract info from</param>
        /// <returns>Basic GST information extracted from the number structure</returns>
        [HttpGet("extract/{gstNumber}")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<GSTExtractedInfoDto>> ExtractGSTInfo(
            [Required] [StringLength(15, MinimumLength = 15)] string gstNumber)
        {
            try
            {
                var extractedInfo = _gstValidationService.ExtractGSTInfo(gstNumber.ToUpperInvariant());
                return Ok(ApiResponse<GSTExtractedInfoDto>.SuccessResponse(extractedInfo, "GST information extracted successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<GSTExtractedInfoDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting GST information");
                return StatusCode(500, ApiResponse<GSTExtractedInfoDto>.ErrorResponse("An error occurred while extracting GST information"));
            }
        }
    }
}
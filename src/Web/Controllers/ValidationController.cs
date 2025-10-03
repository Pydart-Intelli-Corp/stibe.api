using Microsoft.AspNetCore.Mvc;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs;
using stibe.api.Services.Interfaces;

namespace stibe.api.src.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidationController : ControllerBase
    {
        private readonly IGSTValidationService _gstValidationService;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            IGSTValidationService gstValidationService,
            ILogger<ValidationController> logger)
        {
            _gstValidationService = gstValidationService;
            _logger = logger;
        }

        [HttpPost("gst")]
        public async Task<ActionResult<ApiResponse<GSTValidationDto>>> ValidateGST([FromBody] GSTLookupRequestDto request)
        {
            try
            {
                _logger.LogInformation("GST validation requested for: {GSTNumber}", request.GSTNumber);

                if (string.IsNullOrWhiteSpace(request.GSTNumber))
                {
                    return BadRequest(ApiResponse<GSTValidationDto>.ErrorResponse(
                        "GST number is required"));
                }

                var result = await _gstValidationService.ValidateGSTAsync(request.GSTNumber);

                _logger.LogInformation("GST validation completed for: {GSTNumber}, Valid: {IsValid}", 
                    request.GSTNumber, result.IsValid);

                return Ok(ApiResponse<GSTValidationDto>.SuccessResponse(result, "GST validation completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating GST number: {GSTNumber}", request.GSTNumber);
                return StatusCode(500, ApiResponse<GSTValidationDto>.ErrorResponse(
                    "An error occurred while validating GST number"));
            }
        }

        [HttpGet("gst/{gstNumber}")]
        public async Task<ActionResult<ApiResponse<GSTValidationDto>>> ValidateGSTByPath(string gstNumber)
        {
            try
            {
                _logger.LogInformation("GST validation requested via path for: {GSTNumber}", gstNumber);

                if (string.IsNullOrWhiteSpace(gstNumber))
                {
                    return BadRequest(ApiResponse<GSTValidationDto>.ErrorResponse(
                        "GST number is required"));
                }

                var result = await _gstValidationService.ValidateGSTAsync(gstNumber);

                _logger.LogInformation("GST validation completed for: {GSTNumber}, Valid: {IsValid}", 
                    gstNumber, result.IsValid);

                return Ok(ApiResponse<GSTValidationDto>.SuccessResponse(result, "GST validation completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating GST number: {GSTNumber}", gstNumber);
                return StatusCode(500, ApiResponse<GSTValidationDto>.ErrorResponse(
                    "An error occurred while validating GST number"));
            }
        }

        [HttpGet("gst/{gstNumber}/details")]
        public async Task<ActionResult<ApiResponse<GSTDetailsDto>>> GetGSTDetails(string gstNumber)
        {
            try
            {
                _logger.LogInformation("GST details requested for: {GSTNumber}", gstNumber);

                if (string.IsNullOrWhiteSpace(gstNumber))
                {
                    return BadRequest(ApiResponse<GSTDetailsDto>.ErrorResponse(
                        "GST number is required"));
                }

                var result = await _gstValidationService.GetGSTDetailsAsync(gstNumber);

                if (result == null)
                {
                    return NotFound(ApiResponse<GSTDetailsDto>.ErrorResponse(
                        "GST details not found"));
                }

                _logger.LogInformation("GST details retrieved for: {GSTNumber}", gstNumber);

                return Ok(ApiResponse<GSTDetailsDto>.SuccessResponse(result, "GST details retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving GST details: {GSTNumber}", gstNumber);
                return StatusCode(500, ApiResponse<GSTDetailsDto>.ErrorResponse(
                    "An error occurred while retrieving GST details"));
            }
        }
    }
}
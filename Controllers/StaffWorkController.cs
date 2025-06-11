using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs.Features;
using stibe.api.Models.DTOs.PartnersDTOs.StaffsDTOs;
using stibe.api.Services.Interfaces.Partner;
using System.Security.Claims;

namespace stibe.api.Controllers
{
    [ApiController]
    [Route("api/staff/work")]
    [Authorize(Roles = "Staff")]
    public class StaffWorkController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IStaffWorkService _staffWorkService;
        private readonly ILogger<StaffWorkController> _logger;

        public StaffWorkController(
            ApplicationDbContext context,
            IStaffWorkService staffWorkService,
            ILogger<StaffWorkController> logger)
        {
            _context = context;
            _staffWorkService = staffWorkService;
            _logger = logger;
        }
        [HttpPost("clock-in")]
        public async Task<ActionResult<ApiResponse<StaffWorkStatusDto>>> ClockIn(StaffClockRequestDto request)
        {
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                if (staffId == null)
                {
                    _logger.LogWarning("Staff profile not found for current user");
                    return NotFound(ApiResponse<StaffWorkStatusDto>.ErrorResponse("Staff profile not found"));
                }

                // Validate manual date if provided
                if (request.WorkDate.HasValue)
                {
                    if (request.WorkDate.Value.Date > DateTime.Today.AddDays(1))
                    {
                        return BadRequest(ApiResponse<StaffWorkStatusDto>.ErrorResponse("Work date cannot be more than 1 day in the future"));
                    }
                    if (request.WorkDate.Value.Date < DateTime.Today.AddDays(-30))
                    {
                        return BadRequest(ApiResponse<StaffWorkStatusDto>.ErrorResponse("Work date cannot be more than 30 days in the past"));
                    }
                }

                _logger.LogInformation($"Attempting clock-in for staff {staffId} on {request.WorkDate?.Date.ToString("yyyy-MM-dd") ?? "today"}");

                var success = await _staffWorkService.ClockInAsync(staffId.Value, request);
                if (!success)
                {
                    var dateStr = request.WorkDate?.Date.ToString("yyyy-MM-dd") ?? "today";
                    _logger.LogWarning($"Clock-in failed for staff {staffId} on {dateStr}");
                    return BadRequest(ApiResponse<StaffWorkStatusDto>.ErrorResponse($"Failed to clock in for {dateStr}. You may already be clocked in or have an active session."));
                }

                // Get work status for the specified date
                var workStatus = await _staffWorkService.GetWorkStatusForDateAsync(staffId.Value, request.WorkDate?.Date ?? DateTime.Today);

                var dateMessage = request.WorkDate.HasValue ? $" for {request.WorkDate.Value:yyyy-MM-dd}" : "";
                var timeMessage = request.ClockTime.HasValue ? $" at {request.ClockTime.Value:hh\\:mm}" : "";

                _logger.LogInformation($"Staff {staffId} clocked in successfully{dateMessage}{timeMessage}");
                return Ok(ApiResponse<StaffWorkStatusDto>.SuccessResponse(workStatus,
                    $"Clocked in successfully{dateMessage}{timeMessage}! Have a great day at work! 💪"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock in: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<StaffWorkStatusDto>.ErrorResponse($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("clock-out")]
        public async Task<ActionResult<ApiResponse<StaffWorkStatusDto>>> ClockOut(StaffClockRequestDto request)
        {
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                if (staffId == null)
                {
                    return NotFound(ApiResponse<StaffWorkStatusDto>.ErrorResponse("Staff profile not found"));
                }

                var success = await _staffWorkService.ClockOutAsync(staffId.Value, request);
                if (!success)
                {
                    var dateStr = request.WorkDate?.Date.ToString("yyyy-MM-dd") ?? "today";
                    return BadRequest(ApiResponse<StaffWorkStatusDto>.ErrorResponse($"Failed to clock out for {dateStr}. You may not be clocked in."));
                }

                // Get work status for the specified date
                var workStatus = await _staffWorkService.GetWorkStatusForDateAsync(staffId.Value, request.WorkDate?.Date ?? DateTime.Today);

                var hoursWorked = workStatus.WorkedMinutes / 60.0;
                var dateMessage = request.WorkDate.HasValue ? $" for {request.WorkDate.Value:yyyy-MM-dd}" : "";
                var timeMessage = request.ClockTime.HasValue ? $" at {request.ClockTime.Value:hh\\:mm}" : "";

                _logger.LogInformation($"Staff {staffId} clocked out successfully{dateMessage}{timeMessage}");
                return Ok(ApiResponse<StaffWorkStatusDto>.SuccessResponse(workStatus,
                    $"Clocked out successfully{dateMessage}{timeMessage}! You worked {hoursWorked:F1} hours. Great job! 🎉"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock out: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<StaffWorkStatusDto>.ErrorResponse($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult<ApiResponse<StaffWorkStatusDto>>> GetWorkStatus()
        {
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                if (staffId == null)
                {
                    _logger.LogWarning("Staff profile not found for current user");
                    return NotFound(ApiResponse<StaffWorkStatusDto>.ErrorResponse("Staff profile not found"));
                }

                _logger.LogInformation($"Getting work status for staff {staffId}");

                var workStatus = await _staffWorkService.GetCurrentWorkStatusAsync(staffId.Value);
                return Ok(ApiResponse<StaffWorkStatusDto>.SuccessResponse(workStatus));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting work status: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<StaffWorkStatusDto>.ErrorResponse($"An error occurred: {ex.Message}"));
            }
        }

        [HttpGet("today-session")]
        public async Task<ActionResult<ApiResponse<WorkSessionResponseDto>>> GetTodaySession()
        {
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                if (staffId == null)
                {
                    return NotFound(ApiResponse<WorkSessionResponseDto>.ErrorResponse("Staff profile not found"));
                }

                var session = await _staffWorkService.GetTodayWorkSessionAsync(staffId.Value);
                if (session == null)
                {
                    return Ok(ApiResponse<WorkSessionResponseDto>.SuccessResponse(null, "No work session found for today"));
                }

                return Ok(ApiResponse<WorkSessionResponseDto>.SuccessResponse(session));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's session");
                return StatusCode(500, ApiResponse<WorkSessionResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<WorkSessionHistoryResponseDto>>> GetWorkHistory(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                if (staffId == null)
                {
                    return NotFound(ApiResponse<WorkSessionHistoryResponseDto>.ErrorResponse("Staff profile not found"));
                }

                var request = new WorkSessionHistoryRequestDto
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Page = page,
                    PageSize = pageSize
                };

                var history = await _staffWorkService.GetWorkSessionHistoryAsync(staffId.Value, request);
                return Ok(ApiResponse<WorkSessionHistoryResponseDto>.SuccessResponse(history,
                    $"Found {history.TotalCount} work sessions"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting work history");
                return StatusCode(500, ApiResponse<WorkSessionHistoryResponseDto>.ErrorResponse("An error occurred"));
            }
        }

        [HttpPost("start-break")]
        public async Task<ActionResult<ApiResponse>> StartBreak(BreakRequestDto request)
        {
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                if (staffId == null)
                {
                    return NotFound(ApiResponse.ErrorResponse("Staff profile not found"));
                }

                var success = await _staffWorkService.StartBreakAsync(staffId.Value, request);
                if (!success)
                {
                    return BadRequest(ApiResponse.ErrorResponse("Failed to start break"));
                }

                return Ok(ApiResponse.SuccessResponse($"Started {request.BreakType.ToLower()} break. Enjoy your break! ☕"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting break");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred"));
            }
        }

        [HttpPost("end-break")]
        public async Task<ActionResult<ApiResponse>> EndBreak(BreakRequestDto request)
        {
            try
            {
                var staffId = await GetCurrentStaffIdAsync();
                if (staffId == null)
                {
                    return NotFound(ApiResponse.ErrorResponse("Staff profile not found"));
                }

                var success = await _staffWorkService.EndBreakAsync(staffId.Value, request);
                if (!success)
                {
                    return BadRequest(ApiResponse.ErrorResponse("Failed to end break"));
                }

                return Ok(ApiResponse.SuccessResponse("Break ended. Welcome back! Ready for the next customer! 💪"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending break");
                return StatusCode(500, ApiResponse.ErrorResponse("An error occurred"));
            }
        }

        // Helper methods
        private async Task<int?> GetCurrentStaffIdAsync()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return null;

            var staff = await _context.Staff
                .FirstOrDefaultAsync(s => s.UserId == currentUserId.Value);

            return staff?.Id;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }
    }
}
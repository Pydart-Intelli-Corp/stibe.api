using stibe.api.Models.DTOs;

namespace stibe.api.Services.Interfaces
{
    public interface IStaffWorkService
    {
        // Clock management
        Task<bool> ClockInAsync(int staffId, StaffClockRequestDto request);
        Task<bool> ClockOutAsync(int staffId, StaffClockRequestDto request);
        Task<StaffWorkStatusDto> GetCurrentWorkStatusAsync(int staffId);
        Task<bool> IsStaffClockedInAsync(int staffId);

        // Work session management
        Task<WorkSessionResponseDto?> GetTodayWorkSessionAsync(int staffId);
        Task<WorkSessionHistoryResponseDto> GetWorkSessionHistoryAsync(int staffId, WorkSessionHistoryRequestDto request);

        // Break management
        Task<bool> StartBreakAsync(int staffId, BreakRequestDto request);
        Task<bool> EndBreakAsync(int staffId, BreakRequestDto request);
        Task<bool> IsStaffOnBreakAsync(int staffId);

        // Status tracking
        Task<string> GetStaffCurrentStatusAsync(int staffId);
        Task UpdateWorkSessionMetricsAsync(int staffId);

        // Validation
        Task<bool> ValidateClockInLocationAsync(int staffId, decimal? latitude, decimal? longitude);
        Task<string> GetLocationStatusAsync(decimal? latitude, decimal? longitude, int salonId);

        Task<StaffWorkStatusDto> GetWorkStatusForDateAsync(int staffId, DateTime date);
    }
}
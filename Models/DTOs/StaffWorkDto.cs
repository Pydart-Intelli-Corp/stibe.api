using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs
{
    public class StaffClockRequestDto
    {
        [Required]
        [StringLength(10)]
        public string Action { get; set; } = string.Empty; // "ClockIn", "ClockOut"

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Add manual date selection (optional - defaults to today)
        public DateTime? WorkDate { get; set; }

        // Add time override (optional - defaults to current time)
        public TimeSpan? ClockTime { get; set; }
    }
    public class StaffWorkStatusDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty; // "Available", "InService", "OnBreak", "OffShift"
        public bool IsClockedIn { get; set; }
        public DateTime WorkDate { get; set; }
        public TimeSpan? ClockInTime { get; set; }
        public TimeSpan? ClockOutTime { get; set; }
        public int ScheduledMinutes { get; set; }
        public int WorkedMinutes { get; set; }
        public int BreakMinutes { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public TimeSpan? NextBreakTime { get; set; }
        public bool IsOnBreak { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
        public string LocationStatus { get; set; } = string.Empty;
    }

    public class WorkSessionResponseDto
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public TimeSpan ClockInTime { get; set; }
        public TimeSpan? ClockOutTime { get; set; }
        public int ScheduledMinutes { get; set; }
        public int ActualMinutes { get; set; }
        public int BreakMinutes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal? ClockInLatitude { get; set; }
        public decimal? ClockInLongitude { get; set; }
        public decimal? ClockOutLatitude { get; set; }
        public decimal? ClockOutLongitude { get; set; }
        public int ServicesCompleted { get; set; }
        public decimal RevenueGenerated { get; set; }
        public decimal CommissionEarned { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WorkSessionHistoryRequestDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class WorkSessionHistoryResponseDto
    {
        public List<WorkSessionResponseDto> Sessions { get; set; } = new List<WorkSessionResponseDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public WorkSessionSummaryDto Summary { get; set; } = null!;
    }

    public class WorkSessionSummaryDto
    {
        public int TotalDays { get; set; }
        public int DaysWorked { get; set; }
        public decimal AttendancePercentage { get; set; }
        public TimeSpan TotalScheduledTime { get; set; }
        public TimeSpan TotalWorkedTime { get; set; }
        public TimeSpan AverageWorkDay { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public int TotalServices { get; set; }
        public decimal AverageUtilization { get; set; }
        public List<string> Highlights { get; set; } = new List<string>();
    }

    public class BreakRequestDto
    {
        [Required]
        [StringLength(10)]
        public string Action { get; set; } = string.Empty; // "StartBreak", "EndBreak"

        [StringLength(20)]
        public string BreakType { get; set; } = "Regular"; // "Regular", "Lunch", "Emergency"

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
    }
}
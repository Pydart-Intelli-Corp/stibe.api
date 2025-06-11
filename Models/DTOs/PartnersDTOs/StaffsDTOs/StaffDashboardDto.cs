using System.ComponentModel.DataAnnotations;

namespace stibe.api.Models.DTOs.PartnersDTOs.StaffsDTOs
{
    public class StaffDashboardResponseDto
    {
        public StaffProfileSummaryDto Profile { get; set; } = null!;
        public TodayWorkSummaryDto TodayWork { get; set; } = null!;
        public List<TodayBookingDto> TodayBookings { get; set; } = new List<TodayBookingDto>();
        public StaffPerformanceDto Performance { get; set; } = null!;
        public List<StaffNotificationDto> Notifications { get; set; } = new List<StaffNotificationDto>();
        public StaffTargetDto MonthlyTarget { get; set; } = null!;
    }

    public class StaffProfileSummaryDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Role { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public string SalonName { get; set; } = string.Empty;
        public bool IsOnShift { get; set; }
        public TimeSpan ShiftStart { get; set; }
        public TimeSpan ShiftEnd { get; set; }
        public string CurrentStatus { get; set; } = string.Empty; // "Available", "InService", "OnBreak", "OffShift"
    }

    public class TodayWorkSummaryDto
    {
        public DateTime WorkDate { get; set; }
        public TimeSpan? ClockInTime { get; set; }
        public TimeSpan? ClockOutTime { get; set; }
        public bool IsClockedIn { get; set; }
        public int ScheduledMinutes { get; set; }
        public int WorkedMinutes { get; set; }
        public int ServicesCompleted { get; set; }
        public int ServicesRemaining { get; set; }
        public decimal TodayEarnings { get; set; }
        public decimal TodayCommission { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public TimeSpan? NextBreakTime { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class TodayBookingDto
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerPhoto { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public TimeSpan ScheduledTime { get; set; }
        public TimeSpan EstimatedStartTime { get; set; }
        public TimeSpan EstimatedEndTime { get; set; }
        public int DurationMinutes { get; set; }
        public decimal ServicePrice { get; set; }
        public decimal ExpectedCommission { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ServiceStatus { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsRunningLate { get; set; }
        public int DelayMinutes { get; set; }
        public bool IsNextBooking { get; set; }
        public bool CanStartEarly { get; set; }
        public string CustomerPreferences { get; set; } = string.Empty;
    }

    public class StaffPerformanceDto
    {
        public int TodayServices { get; set; }
        public decimal TodayEarnings { get; set; }
        public int WeekServices { get; set; }
        public decimal WeekEarnings { get; set; }
        public int MonthServices { get; set; }
        public decimal MonthEarnings { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int UtilizationPercentage { get; set; }
        public List<string> TopServices { get; set; } = new List<string>();
        public List<string> Achievements { get; set; } = new List<string>();
    }

    public class StaffNotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;
    }

    public class StaffTargetDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;

        // Revenue
        public decimal TargetRevenue { get; set; }
        public decimal ActualRevenue { get; set; }
        public decimal RevenueProgress { get; set; }

        // Services
        public int TargetServices { get; set; }
        public int ActualServices { get; set; }
        public decimal ServiceProgress { get; set; }

        // Rating
        public decimal TargetRating { get; set; }
        public decimal ActualRating { get; set; }

        // Bonus
        public decimal PotentialBonus { get; set; }
        public bool BonusEligible { get; set; }
        public string BonusStatus { get; set; } = string.Empty;

        public List<string> Achievements { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }
}
using Microsoft.EntityFrameworkCore;
using stibe.api.Data;
using stibe.api.Models.DTOs;
using stibe.api.Models.Entities;
using stibe.api.Services.Interfaces;

namespace stibe.api.Services.Implementations
{
    public class StaffWorkService : IStaffWorkService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationService _locationService;
        private readonly ILogger<StaffWorkService> _logger;

        public StaffWorkService(
            ApplicationDbContext context,
            ILocationService locationService,
            ILogger<StaffWorkService> logger)
        {
            _context = context;
            _locationService = locationService;
            _logger = logger;
        }

        public async Task<bool> ClockInAsync(int staffId, StaffClockRequestDto request)
        {
            try
            {
                // Use provided date or default to today
                var workDate = request.WorkDate?.Date ?? DateTime.Today;

                // Use provided time or default to current time
                var clockTime = request.ClockTime ?? DateTime.Now.TimeOfDay;

                // Validate date is not too far in the future
                if (workDate > DateTime.Today.AddDays(1))
                {
                    _logger.LogWarning($"Work date {workDate:yyyy-MM-dd} is too far in the future");
                    return false;
                }

                // Validate date is not too far in the past (e.g., more than 30 days)
                if (workDate < DateTime.Today.AddDays(-30))
                {
                    _logger.LogWarning($"Work date {workDate:yyyy-MM-dd} is too far in the past");
                    return false;
                }

                var staff = await _context.Staff
                    .Include(s => s.Salon)
                    .FirstOrDefaultAsync(s => s.Id == staffId);

                if (staff == null)
                {
                    _logger.LogWarning($"Staff not found: {staffId}");
                    return false;
                }

                // Only check if there's an ACTIVE session (not clocked out yet) for this specific date
                var activeSession = await _context.StaffWorkSessions
                    .FirstOrDefaultAsync(s => s.StaffId == staffId &&
                                            s.WorkDate.Date == workDate &&
                                            s.ClockOutTime == null &&
                                            s.Status == "Active");

                if (activeSession != null)
                {
                    _logger.LogWarning($"Staff {staffId} already has an active session on {workDate:yyyy-MM-dd} (not clocked out yet)");
                    return false;
                }

                // Validate location if provided
                string locationStatus = "Location not provided";
                if (request.Latitude.HasValue && request.Longitude.HasValue && staff.Salon != null)
                {
                    locationStatus = await GetLocationStatusAsync(request.Latitude, request.Longitude, staff.SalonId);
                }

                var workSession = new StaffWorkSession
                {
                    StaffId = staffId,
                    WorkDate = workDate,
                    ClockInTime = clockTime,
                    ScheduledMinutes = (int)(staff.EndTime - staff.StartTime).TotalMinutes,
                    Status = "Active",
                    Notes = request.Notes ?? string.Empty,
                    ClockInLatitude = request.Latitude,
                    ClockInLongitude = request.Longitude
                };

                _context.StaffWorkSessions.Add(workSession);
                await _context.SaveChangesAsync();

                var isManualEntry = request.WorkDate.HasValue || request.ClockTime.HasValue;

                _logger.LogInformation($"=== STAFF CLOCK IN ===");
                _logger.LogInformation($"Staff: {staff.FirstName} {staff.LastName} (ID: {staffId})");
                _logger.LogInformation($"Work Date: {workDate:yyyy-MM-dd} {(request.WorkDate.HasValue ? "(Manual)" : "(Today)")}");
                _logger.LogInformation($"Clock Time: {clockTime:hh\\:mm\\:ss} {(request.ClockTime.HasValue ? "(Manual)" : "(Current)")}");
                _logger.LogInformation($"Entry Type: {(isManualEntry ? "Manual Entry" : "Real-time Entry")}");
                _logger.LogInformation($"Scheduled Hours: {workSession.ScheduledMinutes / 60.0:F1} hours");
                _logger.LogInformation($"Location Status: {locationStatus}");
                if (request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    _logger.LogInformation($"Coordinates: {request.Latitude:F6}, {request.Longitude:F6}");
                }
                _logger.LogInformation($"=== CLOCK IN SUCCESSFUL ===");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clocking in staff {staffId}: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> ClockOutAsync(int staffId, StaffClockRequestDto request)
        {
            try
            {
                // Use provided date or default to today
                var workDate = request.WorkDate?.Date ?? DateTime.Today;

                // Use provided time or default to current time
                var clockTime = request.ClockTime ?? DateTime.Now.TimeOfDay;

                // Find the most recent active session for this date
                var workSession = await _context.StaffWorkSessions
                    .Include(s => s.Staff)
                    .Where(s => s.StaffId == staffId &&
                               s.WorkDate.Date == workDate &&
                               s.ClockOutTime == null &&
                               s.Status == "Active")
                    .OrderByDescending(s => s.ClockInTime)
                    .FirstOrDefaultAsync();

                if (workSession == null)
                {
                    _logger.LogWarning($"No active clock-in found for staff {staffId} on {workDate:yyyy-MM-dd}");
                    return false;
                }

                // Validate clock out time is not before clock in time
                if (clockTime < workSession.ClockInTime)
                {
                    _logger.LogWarning($"Clock out time {clockTime} cannot be before clock in time {workSession.ClockInTime}");
                    return false;
                }

                workSession.ClockOutTime = clockTime;
                workSession.ActualMinutes = (int)(clockTime - workSession.ClockInTime).TotalMinutes;
                workSession.Status = "Completed";
                workSession.Notes += $" | Clock out: {request.Notes}";
                workSession.ClockOutLatitude = request.Latitude;
                workSession.ClockOutLongitude = request.Longitude;

                // Update session metrics
                await UpdateWorkSessionMetricsAsync(staffId, workDate);

                await _context.SaveChangesAsync();

                var hoursWorked = workSession.ActualMinutes / 60.0;
                var utilizationRate = workSession.ScheduledMinutes > 0
                    ? (double)workSession.ActualMinutes / workSession.ScheduledMinutes * 100
                    : 0;

                var isManualEntry = request.WorkDate.HasValue || request.ClockTime.HasValue;

                _logger.LogInformation($"=== STAFF CLOCK OUT ===");
                _logger.LogInformation($"Staff: {workSession.Staff.FirstName} {workSession.Staff.LastName} (ID: {staffId})");
                _logger.LogInformation($"Work Date: {workDate:yyyy-MM-dd} {(request.WorkDate.HasValue ? "(Manual)" : "(Today)")}");
                _logger.LogInformation($"Clock In: {workSession.ClockInTime:hh\\:mm\\:ss}");
                _logger.LogInformation($"Clock Out: {clockTime:hh\\:mm\\:ss} {(request.ClockTime.HasValue ? "(Manual)" : "(Current)")}");
                _logger.LogInformation($"Entry Type: {(isManualEntry ? "Manual Entry" : "Real-time Entry")}");
                _logger.LogInformation($"Hours Worked: {hoursWorked:F1} hours");
                _logger.LogInformation($"Services Completed: {workSession.ServicesCompleted}");
                _logger.LogInformation($"Revenue Generated: ₹{workSession.RevenueGenerated:F2}");
                _logger.LogInformation($"Commission Earned: ₹{workSession.CommissionEarned:F2}");
                _logger.LogInformation($"Utilization Rate: {utilizationRate:F1}%");
                _logger.LogInformation($"=== CLOCK OUT SUCCESSFUL ===");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clocking out staff {staffId}: {ex.Message}");
                return false;
            }
        }
        public async Task<StaffWorkStatusDto> GetCurrentWorkStatusAsync(int staffId)
        {
            try
            {
                var today = DateTime.Today;
                var staff = await _context.Staff
                    .Include(s => s.Salon)
                    .FirstOrDefaultAsync(s => s.Id == staffId);

                if (staff == null)
                    throw new ArgumentException($"Staff not found: {staffId}");

                var workSession = await _context.StaffWorkSessions
                    .FirstOrDefaultAsync(s => s.StaffId == staffId && s.WorkDate.Date == today);

                var currentStatus = await GetStaffCurrentStatusAsync(staffId);
                var isClockedIn = workSession != null && workSession.ClockOutTime == null;
                var isOnBreak = await IsStaffOnBreakAsync(staffId);

                var workedMinutes = 0;
                if (workSession != null)
                {
                    if (workSession.ClockOutTime.HasValue)
                    {
                        workedMinutes = workSession.ActualMinutes;
                    }
                    else if (isClockedIn)
                    {
                        workedMinutes = (int)(DateTime.Now.TimeOfDay - workSession.ClockInTime).TotalMinutes;
                    }
                }

                var scheduledMinutes = (int)(staff.EndTime - staff.StartTime).TotalMinutes;
                var utilizationPercentage = scheduledMinutes > 0 ? (decimal)workedMinutes / scheduledMinutes * 100 : 0;

                var statusMessage = GenerateStatusMessage(currentStatus, isClockedIn, isOnBreak, staff);

                return new StaffWorkStatusDto
                {
                    StaffId = staffId,
                    StaffName = $"{staff.FirstName} {staff.LastName}",
                    Role = staff.Role,
                    CurrentStatus = currentStatus,
                    IsClockedIn = isClockedIn,
                    WorkDate = today,
                    ClockInTime = workSession?.ClockInTime,
                    ClockOutTime = workSession?.ClockOutTime,
                    ScheduledMinutes = scheduledMinutes,
                    WorkedMinutes = workedMinutes,
                    BreakMinutes = workSession?.BreakMinutes ?? 0,
                    UtilizationPercentage = utilizationPercentage,
                    StatusMessage = statusMessage,
                    NextBreakTime = GetNextBreakTime(staff, DateTime.Now.TimeOfDay, isOnBreak),
                    IsOnBreak = isOnBreak,
                    BreakEndTime = isOnBreak ? staff.LunchBreakEnd : null,
                    LocationStatus = "At Salon" // Will be dynamic with real location tracking
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting work status for staff {staffId}: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> IsStaffClockedInAsync(int staffId)
        {
            var today = DateTime.Today;
            return await _context.StaffWorkSessions
                .AnyAsync(s => s.StaffId == staffId &&
                             s.WorkDate.Date == today &&
                             s.ClockOutTime == null);
        }

        public async Task<WorkSessionResponseDto?> GetTodayWorkSessionAsync(int staffId)
        {
            var today = DateTime.Today;
            var session = await _context.StaffWorkSessions
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(s => s.StaffId == staffId && s.WorkDate.Date == today);

            if (session == null)
                return null;

            var utilizationPercentage = session.ScheduledMinutes > 0
                ? (decimal)session.ActualMinutes / session.ScheduledMinutes * 100
                : 0;

            return new WorkSessionResponseDto
            {
                Id = session.Id,
                StaffId = session.StaffId,
                StaffName = $"{session.Staff.FirstName} {session.Staff.LastName}",
                WorkDate = session.WorkDate,
                ClockInTime = session.ClockInTime,
                ClockOutTime = session.ClockOutTime,
                ScheduledMinutes = session.ScheduledMinutes,
                ActualMinutes = session.ActualMinutes,
                BreakMinutes = session.BreakMinutes,
                Status = session.Status,
                Notes = session.Notes,
                ClockInLatitude = session.ClockInLatitude,
                ClockInLongitude = session.ClockInLongitude,
                ClockOutLatitude = session.ClockOutLatitude,
                ClockOutLongitude = session.ClockOutLongitude,
                ServicesCompleted = session.ServicesCompleted,
                RevenueGenerated = session.RevenueGenerated,
                CommissionEarned = session.CommissionEarned,
                UtilizationPercentage = utilizationPercentage,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };
        }

        public async Task<WorkSessionHistoryResponseDto> GetWorkSessionHistoryAsync(int staffId, WorkSessionHistoryRequestDto request)
        {
            var fromDate = request.FromDate ?? DateTime.Today.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.Today;

            var query = _context.StaffWorkSessions
                .Include(s => s.Staff)
                .Where(s => s.StaffId == staffId &&
                           s.WorkDate >= fromDate &&
                           s.WorkDate <= toDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
            var skip = (request.Page - 1) * request.PageSize;

            var sessions = await query
                .OrderByDescending(s => s.WorkDate)
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync();

            var sessionDtos = sessions.Select(session =>
            {
                var utilizationPercentage = session.ScheduledMinutes > 0
                    ? (decimal)session.ActualMinutes / session.ScheduledMinutes * 100
                    : 0;

                return new WorkSessionResponseDto
                {
                    Id = session.Id,
                    StaffId = session.StaffId,
                    StaffName = $"{session.Staff.FirstName} {session.Staff.LastName}",
                    WorkDate = session.WorkDate,
                    ClockInTime = session.ClockInTime,
                    ClockOutTime = session.ClockOutTime,
                    ScheduledMinutes = session.ScheduledMinutes,
                    ActualMinutes = session.ActualMinutes,
                    BreakMinutes = session.BreakMinutes,
                    Status = session.Status,
                    Notes = session.Notes,
                    ClockInLatitude = session.ClockInLatitude,
                    ClockInLongitude = session.ClockInLongitude,
                    ClockOutLatitude = session.ClockOutLatitude,
                    ClockOutLongitude = session.ClockOutLongitude,
                    ServicesCompleted = session.ServicesCompleted,
                    RevenueGenerated = session.RevenueGenerated,
                    CommissionEarned = session.CommissionEarned,
                    UtilizationPercentage = utilizationPercentage,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt
                };
            }).ToList();

            // Calculate summary
            var summary = await CalculateWorkSessionSummaryAsync(staffId, fromDate, toDate);

            return new WorkSessionHistoryResponseDto
            {
                Sessions = sessionDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1,
                Summary = summary
            };
        }

        public async Task<bool> StartBreakAsync(int staffId, BreakRequestDto request)
        {
            // This would implement break tracking
            // For now, we'll just log it
            _logger.LogInformation($"Staff {staffId} started {request.BreakType} break: {request.Notes}");
            return true;
        }

        public async Task<bool> EndBreakAsync(int staffId, BreakRequestDto request)
        {
            // This would implement break tracking
            // For now, we'll just log it
            _logger.LogInformation($"Staff {staffId} ended {request.BreakType} break: {request.Notes}");
            return true;
        }

        public async Task<bool> IsStaffOnBreakAsync(int staffId)
        {
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null) return false;

            var now = DateTime.Now.TimeOfDay;
            return now >= staff.LunchBreakStart && now <= staff.LunchBreakEnd;
        }

        public async Task<string> GetStaffCurrentStatusAsync(int staffId)
        {
            try
            {
                var isClockedIn = await IsStaffClockedInAsync(staffId);
                if (!isClockedIn)
                    return "OffShift";

                var isOnBreak = await IsStaffOnBreakAsync(staffId);
                if (isOnBreak)
                    return "OnBreak";

                // Check if currently serving a customer
                var today = DateTime.Today;
                var now = DateTime.Now.TimeOfDay;

                var currentBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.AssignedStaffId == staffId &&
                                            b.BookingDate.Date == today &&
                                            b.BookingTime <= now &&
                                            b.BookingTime.Add(TimeSpan.FromMinutes(90)) >= now && // Assuming max 90 minute service
                                            (b.Status == "Confirmed" || b.Status == "InProgress"));

                return currentBooking != null ? "InService" : "Available";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting staff status for {staffId}: {ex.Message}");
                return "Unknown";
            }
        }
        public async Task<StaffWorkStatusDto> GetWorkStatusForDateAsync(int staffId, DateTime date)
        {
            try
            {
                var staff = await _context.Staff
                    .Include(s => s.Salon)
                    .FirstOrDefaultAsync(s => s.Id == staffId);

                if (staff == null)
                    throw new ArgumentException($"Staff not found: {staffId}");

                // Get all work sessions for the specified date
                var sessionsForDate = await _context.StaffWorkSessions
                    .Where(s => s.StaffId == staffId && s.WorkDate.Date == date.Date)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                // Get the latest session
                var latestSession = sessionsForDate.FirstOrDefault();

                // Check if currently clocked in (has active session)
                var activeSession = sessionsForDate.FirstOrDefault(s => s.ClockOutTime == null && s.Status == "Active");

                var isClockedIn = activeSession != null;
                var isOnBreak = date.Date == DateTime.Today ? await IsStaffOnBreakAsync(staffId) : false;

                // Calculate total worked minutes for this date (from all sessions)
                var totalWorkedMinutes = 0;
                foreach (var session in sessionsForDate)
                {
                    if (session.ClockOutTime.HasValue)
                    {
                        totalWorkedMinutes += session.ActualMinutes;
                    }
                    else if (session.Status == "Active" && date.Date == DateTime.Today)
                    {
                        // Current active session (only for today)
                        totalWorkedMinutes += (int)(DateTime.Now.TimeOfDay - session.ClockInTime).TotalMinutes;
                    }
                }

                var scheduledMinutes = (int)(staff.EndTime - staff.StartTime).TotalMinutes;
                var utilizationPercentage = scheduledMinutes > 0 ? (decimal)totalWorkedMinutes / scheduledMinutes * 100 : 0;

                var currentStatus = date.Date == DateTime.Today ? await GetStaffCurrentStatusAsync(staffId) : "OffShift";
                var statusMessage = GenerateStatusMessageForDate(currentStatus, isClockedIn, isOnBreak, staff, date);

                return new StaffWorkStatusDto
                {
                    StaffId = staffId,
                    StaffName = $"{staff.FirstName} {staff.LastName}",
                    Role = staff.Role,
                    CurrentStatus = currentStatus,
                    IsClockedIn = isClockedIn,
                    WorkDate = date,
                    ClockInTime = activeSession?.ClockInTime ?? latestSession?.ClockInTime,
                    ClockOutTime = latestSession?.ClockOutTime,
                    ScheduledMinutes = scheduledMinutes,
                    WorkedMinutes = totalWorkedMinutes,
                    BreakMinutes = sessionsForDate.Sum(s => s.BreakMinutes),
                    UtilizationPercentage = utilizationPercentage,
                    StatusMessage = statusMessage,
                    NextBreakTime = date.Date == DateTime.Today ? GetNextBreakTime(staff, DateTime.Now.TimeOfDay, isOnBreak) : null,
                    IsOnBreak = isOnBreak,
                    BreakEndTime = isOnBreak ? staff.LunchBreakEnd : null,
                    LocationStatus = "At Salon"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting work status for staff {staffId} on {date:yyyy-MM-dd}: {ex.Message}");
                throw;
            }
        }

        private string GenerateStatusMessageForDate(string status, bool isClockedIn, bool isOnBreak, Staff staff, DateTime date)
        {
            if (date.Date != DateTime.Today)
            {
                return $"Historical data for {date:yyyy-MM-dd}";
            }

            return status switch
            {
                "OffShift" => $"You haven't clocked in yet. Shift starts at {staff.StartTime:hh\\:mm}",
                "Available" => "Ready for next customer",
                "InService" => "Currently serving a customer",
                "OnBreak" => $"On lunch break until {staff.LunchBreakEnd:hh\\:mm}",
                _ => "Status unknown"
            };
        }
        public async Task UpdateWorkSessionMetricsAsync(int staffId)
        {
            await UpdateWorkSessionMetricsAsync(staffId, DateTime.Today);
        }

        public async Task UpdateWorkSessionMetricsAsync(int staffId, DateTime workDate)
        {
            var workSession = await _context.StaffWorkSessions
                .Where(s => s.StaffId == staffId && s.WorkDate.Date == workDate.Date && s.Status == "Active")
                .OrderByDescending(s => s.ClockInTime)
                .FirstOrDefaultAsync();

            if (workSession == null) return;

            // Get completed bookings for this specific date
            var completedBookings = await _context.Bookings
                .Where(b => b.AssignedStaffId == staffId &&
                           b.BookingDate.Date == workDate.Date &&
                           b.Status == "Completed")
                .ToListAsync();

            workSession.ServicesCompleted = completedBookings.Count;
            workSession.RevenueGenerated = completedBookings.Sum(b => b.TotalAmount);
            workSession.CommissionEarned = completedBookings.Sum(b => b.TotalAmount * 0.4m); // Will use actual commission rate

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ValidateClockInLocationAsync(int staffId, decimal? latitude, decimal? longitude)
        {
            if (!latitude.HasValue || !longitude.HasValue)
                return true; // Allow if no location provided

            var staff = await _context.Staff
                .Include(s => s.Salon)
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (staff?.Salon == null) return true;

            // If salon has coordinates, check distance
            if (staff.Salon.Latitude.HasValue && staff.Salon.Longitude.HasValue)
            {
                var distance = _locationService.CalculateDistance(
                    latitude.Value, longitude.Value,
                    staff.Salon.Latitude.Value, staff.Salon.Longitude.Value);

                // Allow clock in within 200 meters of salon
                return distance <= 0.2; // 200 meters
            }

            return true; // Allow if salon has no coordinates
        }

        public async Task<string> GetLocationStatusAsync(decimal? latitude, decimal? longitude, int salonId)
        {
            if (!latitude.HasValue || !longitude.HasValue)
                return "Location not provided";

            var salon = await _context.Salons.FindAsync(salonId);
            if (salon?.Latitude == null || salon.Longitude == null)
                return "Salon location not configured";

            var distance = _locationService.CalculateDistance(
                latitude.Value, longitude.Value,
                salon.Latitude.Value, salon.Longitude.Value);

            if (distance <= 0.05) // 50 meters
                return "At salon premises";
            else if (distance <= 0.2) // 200 meters  
                return "Near salon";
            else
                return $"Remote location ({distance:F1} km away)";
        }

        // Helper methods
        private string GenerateStatusMessage(string status, bool isClockedIn, bool isOnBreak, Staff staff)
        {
            return status switch
            {
                "OffShift" => $"You haven't clocked in yet. Shift starts at {staff.StartTime:hh\\:mm}",
                "Available" => "Ready for next customer",
                "InService" => "Currently serving a customer",
                "OnBreak" => $"On lunch break until {staff.LunchBreakEnd:hh\\:mm}",
                _ => "Status unknown"
            };
        }

        private TimeSpan? GetNextBreakTime(Staff staff, TimeSpan currentTime, bool isOnBreak)
        {
            if (isOnBreak) return null;

            if (currentTime < staff.LunchBreakStart)
                return staff.LunchBreakStart;

            return null; // No more breaks today
        }

        private async Task<WorkSessionSummaryDto> CalculateWorkSessionSummaryAsync(int staffId, DateTime fromDate, DateTime toDate)
        {
            var sessions = await _context.StaffWorkSessions
                .Where(s => s.StaffId == staffId && s.WorkDate >= fromDate && s.WorkDate <= toDate)
                .ToListAsync();

            var totalDays = (int)(toDate - fromDate).TotalDays + 1;
            var daysWorked = sessions.Count;
            var attendancePercentage = totalDays > 0 ? (decimal)daysWorked / totalDays * 100 : 0;

            var totalScheduledMinutes = sessions.Sum(s => s.ScheduledMinutes);
            var totalWorkedMinutes = sessions.Sum(s => s.ActualMinutes);
            var totalServices = sessions.Sum(s => s.ServicesCompleted);
            var totalRevenue = sessions.Sum(s => s.RevenueGenerated);
            var totalCommission = sessions.Sum(s => s.CommissionEarned);

            var averageWorkDayMinutes = daysWorked > 0 ? totalWorkedMinutes / daysWorked : 0;
            var averageUtilization = totalScheduledMinutes > 0 ? (decimal)totalWorkedMinutes / totalScheduledMinutes * 100 : 0;

            var highlights = new List<string>();
            if (attendancePercentage >= 95) highlights.Add("🎯 Perfect Attendance");
            if (averageUtilization >= 90) highlights.Add("⚡ High Efficiency");
            if (totalServices > daysWorked * 5) highlights.Add("🌟 Service Excellence");

            return new WorkSessionSummaryDto
            {
                TotalDays = totalDays,
                DaysWorked = daysWorked,
                AttendancePercentage = attendancePercentage,
                TotalScheduledTime = TimeSpan.FromMinutes(totalScheduledMinutes),
                TotalWorkedTime = TimeSpan.FromMinutes(totalWorkedMinutes),
                AverageWorkDay = TimeSpan.FromMinutes(averageWorkDayMinutes),
                TotalRevenue = totalRevenue,
                TotalCommission = totalCommission,
                TotalServices = totalServices,
                AverageUtilization = averageUtilization,
                Highlights = highlights
            };
        }
    }
}
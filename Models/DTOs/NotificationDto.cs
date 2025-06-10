using System.ComponentModel.DataAnnotations;

public class StaffNotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // "Schedule", "Performance", "Incentive", "General"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string Priority { get; set; } = string.Empty; // "Low", "Medium", "High", "Urgent"
    public string ActionUrl { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}
// This should be in Models/DTOs/NotificationDto.cs
public class ManualNotificationRequestDto
{
    [Required]
    public int BookingId { get; set; }

    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = string.Empty; // "ReadyNotification", "DelayNotification", "ReminderNotification"

    public int EstimatedMinutesRemaining { get; set; } = 5;

    public bool SendToNext { get; set; } = true;

    public List<string> Channels { get; set; } = new List<string> { "SMS", "Email" }; // "SMS", "Email", "Push", "WhatsApp"

    public bool IncludeWaitTime { get; set; } = true;

    [StringLength(500)]
    public string CustomMessage { get; set; } = string.Empty;

    public bool IncludeDirections { get; set; } = false;
    public bool IncludeParkingInfo { get; set; } = false;
    public bool RequestConfirmation { get; set; } = true;
}

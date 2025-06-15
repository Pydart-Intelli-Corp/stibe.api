// Models/DTOs/PartnersDTOs/ServicesDTOs/ServiceAvailabilityDto.cs
namespace stibe.api.Models.DTOs.PartnersDTOs.ServicesDTOs
{
    public class ServiceAvailabilityDto
    {
        public int? Id { get; set; }
        public int DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int MaxBookingsPerSlot { get; set; } = 1;
        public int SlotDurationMinutes { get; set; } = 30;
        public int BufferTimeMinutes { get; set; } = 0;
    }
}

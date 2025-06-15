// Models/Entities/PartnersEntity/ServicesEntity/ServiceAvailability.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity.ServicesEntity
{
    public class ServiceAvailability : BaseEntity
    {
        public int ServiceId { get; set; }

        public int DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, etc.

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public int MaxBookingsPerSlot { get; set; } = 1;

        public int SlotDurationMinutes { get; set; } = 30;

        public int BufferTimeMinutes { get; set; } = 0;

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;
    }
}

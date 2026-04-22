using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.AppointmentModels
{
    public class AppointmentStatusHistory
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }

        public AppointmentStatus FromStatus { get; set; }
        public AppointmentStatus ToStatus { get; set; }
        public string? Reason { get; set; }
        public Guid? ChangedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Appointment Appointment { get; set; } = null!;
    }
}

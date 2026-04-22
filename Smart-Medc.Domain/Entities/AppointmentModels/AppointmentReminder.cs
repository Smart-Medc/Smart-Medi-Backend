using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.AppointmentModels
{
    public class AppointmentReminder
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }

        public ReminderType ReminderType { get; set; }
        public int MinutesBefore { get; set; } // e.g., 1440 for 24 hours, 60 for 1 hour
        public bool IsSent { get; set; } = false;
        public DateTime? SentAt { get; set; }
        public DateTime ScheduledFor { get; set; }

        public ReminderChannel Channel { get; set; }

        // Navigation
        public virtual Appointment Appointment { get; set; } = null!;
    }




}

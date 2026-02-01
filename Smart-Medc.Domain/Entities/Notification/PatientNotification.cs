using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Enums;
namespace Smart_Medc.Domain.Entities.Notification
{
    public class PatientNotification
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }

        public PatientNotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? Data { get; set; } // JSON for additional data

        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Patient Patient { get; set; } = null!;
    }


}

using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.Notification
{
    public class OrganizationNotification
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }

        public OrganizationNotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? Data { get; set; } // JSON

        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
    }

    
}

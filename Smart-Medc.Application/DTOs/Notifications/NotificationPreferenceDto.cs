using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.Notifications
{
    public class NotificationPreferenceDto
    {
        public Guid Id { get; set; }
        public NotificationType NotificationType { get; set; }
        public string TypeDisplayName { get; set; } = string.Empty;
        public bool EmailEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public bool PushEnabled { get; set; }
    }

    public class UpdateNotificationPreferenceDto
    {
        public NotificationType NotificationType { get; set; }
        public bool EmailEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public bool PushEnabled { get; set; }
    }

    public class UpdateAllNotificationPreferencesDto
    {
        public List<UpdateNotificationPreferenceDto> Preferences { get; set; } = new();
    }

    public class CreatePatientNotificationDto
    {
        public Guid PatientId { get; set; }
        public PatientNotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? Data { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    }

    public class CreateOrganizationNotificationDto
    {
        public Guid OrganizationId { get; set; }
        public OrganizationNotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? Data { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    }

    public class MarkReadBatchDto
    {
        public List<Guid> NotificationIds { get; set; } = new();
    }
}

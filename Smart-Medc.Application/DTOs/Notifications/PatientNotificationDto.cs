using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.Notifications
{
    public class PatientNotificationDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public PatientNotificationType Type { get; set; }
        public string TypeDisplayName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? Data { get; set; }
        public NotificationPriority Priority { get; set; }
        public string PriorityDisplayName { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PatientNotificationListDto
    {
        public List<PatientNotificationDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }

    public class PatientUnreadCountDto
    {
        public int TotalUnread { get; set; }
        public int AppointmentReminders { get; set; }
        public int MedicationReminders { get; set; }
        public int AppointmentUpdates { get; set; }
        public int RecordAccess { get; set; }
        public int AIAlerts { get; set; }
        public int System { get; set; }
    }
}

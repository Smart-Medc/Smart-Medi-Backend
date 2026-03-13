using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.Notifications
{
    public class OrganizationNotificationDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public OrganizationNotificationType Type { get; set; }
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

    public class OrganizationNotificationListDto
    {
        public List<OrganizationNotificationDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }

    public class OrganizationUnreadCountDto
    {
        public int TotalUnread { get; set; }
        public int NewAppointmentRequests { get; set; }
        public int AppointmentCancellations { get; set; }
        public int UpcomingAppointments { get; set; }
        public int PatientNoShows { get; set; }
        public int AppointmentRescheduled { get; set; }
        public int System { get; set; }
    }
}

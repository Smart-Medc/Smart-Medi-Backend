namespace Smart_Medc.Application.DTOs.Patient
{
    public class NotificationPreferenceDto
    {
        public Guid Id { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string NotificationTypeDisplay { get; set; } = string.Empty;
        public string NotificationDescription { get; set; } = string.Empty;
        public bool EmailEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public bool PushEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
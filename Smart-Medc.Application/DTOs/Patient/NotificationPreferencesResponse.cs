namespace Smart_Medc.Application.DTOs.Patient
{
    public class NotificationPreferencesResponse
    {
        public List<NotificationPreferenceDto> Preferences { get; set; } = new();
        public bool AllEmailEnabled { get; set; }
        public bool AllSmsEnabled { get; set; }
        public bool AllPushEnabled { get; set; }
    }
}
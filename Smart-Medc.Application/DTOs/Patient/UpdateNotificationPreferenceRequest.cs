using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Patient
{
    public class UpdateNotificationPreferenceRequest
    {
        [Required(ErrorMessage = "Notification type is required")]
        public string NotificationType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email enabled status is required")]
        public bool EmailEnabled { get; set; }

        [Required(ErrorMessage = "SMS enabled status is required")]
        public bool SmsEnabled { get; set; }

        [Required(ErrorMessage = "Push enabled status is required")]
        public bool PushEnabled { get; set; }
    }
}
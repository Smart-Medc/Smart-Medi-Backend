using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Patient
{
    /// <summary>
    /// Request to update all notification preferences at once
    /// </summary>
    public class UpdateAllNotificationPreferencesRequest
    {
        [Required(ErrorMessage = "Email preferences required")]
        public bool AllEmailEnabled { get; set; }

        [Required(ErrorMessage = "SMS preferences required")]
        public bool AllSmsEnabled { get; set; }

        [Required(ErrorMessage = "Push preferences required")]
        public bool AllPushEnabled { get; set; }
    }
}
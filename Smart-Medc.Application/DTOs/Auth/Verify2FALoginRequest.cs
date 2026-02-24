using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Auth
{
    public class Verify2FALoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be numeric")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Remember this device to skip 2FA for 30 days
        /// </summary>
        public bool RememberDevice { get; set; } = false;
    }
}
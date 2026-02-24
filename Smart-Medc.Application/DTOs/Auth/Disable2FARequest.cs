using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Auth
{
    public class Disable2FARequest
    {
        [Required(ErrorMessage = "Password is required for security verification")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code from authenticator is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be numeric")]
        public string Code { get; set; } = string.Empty;
    }
}
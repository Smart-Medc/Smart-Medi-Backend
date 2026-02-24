using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Auth
{
    public class Verify2FASetupRequest
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be numeric")]
        public string Code { get; set; } = string.Empty;
    }
}
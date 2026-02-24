using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Auth
{
    public class VerifyPasswordResetCodeRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be numeric")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Auth
{
    public class TwoFactorVerificationRequest
    {
        [Required(ErrorMessage = "User ID is required")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(10, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;

        public bool RememberDevice { get; set; } = false;
    }
}
using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Auth
{
    public class Enable2FARequest
    {
        [Required(ErrorMessage = "Password is required for security verification")]
        public string Password { get; set; } = string.Empty;
    }
}
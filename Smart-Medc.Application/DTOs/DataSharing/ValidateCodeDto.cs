using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.DataSharing
{
    // Viewer Side (Organization/Anonymous)
    public class ValidateCodeDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}

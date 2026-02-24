
namespace Smart_Medc.Application.DTOs.DataSharing
{
    public class ValidateCodeResponseDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public int RemainingTimeSeconds { get; set; }
        public string? PatientName { get; set; }
    }
}

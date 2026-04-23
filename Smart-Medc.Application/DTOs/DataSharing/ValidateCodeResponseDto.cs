
namespace Smart_Medc.Application.DTOs.DataSharing
{
    public class ValidateCodeResponseDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public int RemainingTimeSeconds { get; set; }

        // Already existed
        public string? PatientName { get; set; }

        // ADDED: extra patient fields needed by the viewer page.
        public int? PatientAge { get; set; }
        public string? PatientGender { get; set; }
        public string? PatientBloodType { get; set; }
        public string? PatientEmail { get; set; }
        public string? PatientPhone { get; set; }
        public DateTime? PatientDateOfBirth { get; set; }
    }
}

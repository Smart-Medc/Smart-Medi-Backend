namespace Smart_Medc.Application.DTOs.Patient
{
    public class ProfilePhotoResponse
    {
        public string? PhotoUrl { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OldPhotoUrl { get; set; }
    }
}
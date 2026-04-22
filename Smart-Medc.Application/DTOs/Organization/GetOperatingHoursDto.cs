namespace Smart_Medc.Application.DTOs.Organization
{
    public class GetOperatingHoursDto
    {
        public string Day { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public string? OpenTime { get; set; }  // "HH:mm" format
        public string? CloseTime { get; set; } // "HH:mm" format
    }
}

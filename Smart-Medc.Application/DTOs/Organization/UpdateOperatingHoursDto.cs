
namespace Smart_Medc.Application.DTOs.Organization
{
    public class UpdateOperatingHoursDto
    {
        public string Day { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public string? OpenTime { get; set; }  // "HH:mm" format, null when IsOpen=false
        public string? CloseTime { get; set; } // "HH:mm" format, null when IsOpen=false
    }

    public class UpdateOperatingHoursRequestDto
    {
        public List<UpdateOperatingHoursDto> Schedule { get; set; } = new();
    }
}

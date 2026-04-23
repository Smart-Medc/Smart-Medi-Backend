
namespace Smart_Medc.Application.DTOs.Organization
{
    public class AvailabilityExceptionDto
    {
        public Guid Id { get; set; }
        public string Date { get; set; } = string.Empty;   // "yyyy-MM-dd"
        public bool IsFullDayOff { get; set; }
        public string? StartTime { get; set; }             // "HH:mm" or null
        public string? EndTime { get; set; }               // "HH:mm" or null
        public string? Reason { get; set; }
    }
}

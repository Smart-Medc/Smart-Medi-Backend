
namespace Smart_Medc.Application.DTOs.Organization
{
    public class UpsertAvailabilityExceptionDto
    {
        public bool IsFullDayOff { get; set; }
        public string? StartTime { get; set; }  // "HH:mm", required when IsFullDayOff=false
        public string? EndTime { get; set; }    // "HH:mm", required when IsFullDayOff=false
        public string? Reason { get; set; }
    }
}

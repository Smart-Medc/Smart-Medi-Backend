
namespace Smart_Medc.Application.DTOs.Organization
{
    public class TimeSlotDto
    {
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Status { get; set; } = "Available"; // Available, Booked
    }
}

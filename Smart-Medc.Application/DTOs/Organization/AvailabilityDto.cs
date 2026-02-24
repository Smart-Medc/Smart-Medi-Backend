
namespace Smart_Medc.Application.DTOs.Organization
{
    // Availability
    public class AvailabilityDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = "Available"; // Available, Closed
    }
}

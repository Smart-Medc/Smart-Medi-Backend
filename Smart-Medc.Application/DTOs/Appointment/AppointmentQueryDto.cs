
namespace Smart_Medc.Application.DTOs.Appointment
{
    // Org Specific Inputs
    public class AppointmentQueryDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; } // Changed to string for flexibility
        //public int PageNumber { get; set; } = 1;
        //public int PageSize { get; set; } = 10;
    }
}

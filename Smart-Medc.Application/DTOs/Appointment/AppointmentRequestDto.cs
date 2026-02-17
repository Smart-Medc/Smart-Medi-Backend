
namespace Smart_Medc.Application.DTOs.Appointment
{
    public class AppointmentRequestDto : AppointmentDto
    {
        public DateTime RequestedAt { get; set; }
        public bool HasSharedRecords { get; set; }
    }
}

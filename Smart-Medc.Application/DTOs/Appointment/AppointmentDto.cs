
namespace Smart_Medc.Application.DTOs.Appointment
{
    // Outputs
    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? DoctorName { get; set; }
        public DateTime Date { get; set; }
        public TimeOnly Time { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string VisitType { get; set; } = string.Empty;
    }
}

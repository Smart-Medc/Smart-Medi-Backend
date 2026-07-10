
namespace Smart_Medc.Application.DTOs.Appointment
{
    public class BookAppointmentDto
    {
        public Guid OrganizationId { get; set; }
        // PatientId REMOVED - should come from authenticated user context
        public Guid? DoctorId { get; set; }
        public DateTime Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public string VisitType { get; set; } = string.Empty; // Changed to string for validation
        public string Reason { get; set; } = string.Empty;
        public bool ShareRecords { get; set; }
        public List<Guid> RecordsToShare { get; set; } = new(); // Specific records to share

        // ADDED: expiration type for the share code generated at booking time.
        // Defaults to ThirtyDays to match the previous hardcoded value so
        public string ExpirationType { get; set; } = "ThirtyDays";
    }
}

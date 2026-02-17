
namespace Smart_Medc.Application.DTOs.Appointment
{
    public class AppointmentDetailDto : AppointmentDto
    {
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool IsRecordsShared { get; set; }
        public string? AccessCode { get; set; }
        public string CancellationPolicy { get; set; } = string.Empty;
        public string? PreparationInstructions { get; set; }
        public List<AppointmentHistoryItemDto> History { get; set; } = new();
    }

    public class AppointmentHistoryItemDto
    {
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Reason { get; set; }
    }
}

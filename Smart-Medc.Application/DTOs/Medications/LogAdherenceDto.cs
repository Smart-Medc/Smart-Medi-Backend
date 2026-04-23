using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.Medications
{
    public class LogAdherenceDto
    {
        public DateTime ScheduledTime { get; set; }
        public DateTime? TakenTime { get; set; }
        public AdherenceStatus Status { get; set; }
    }
}

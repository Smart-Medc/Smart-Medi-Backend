namespace Smart_Medc.Application.DTOs.Medications
{
    public class ReminderDto
    {
        public Guid Id { get; set; }
        public TimeOnly ReminderTime { get; set; }
        public string? DaysOfWeek { get; set; }
        public bool IsActive { get; set; }
    }
}

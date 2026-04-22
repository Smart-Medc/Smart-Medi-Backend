namespace Smart_Medc.Application.DTOs.Medications
{
    public class UpdateReminderDto
    {
        public TimeOnly ReminderTime { get; set; }
        public string? DaysOfWeek { get; set; }
        public bool IsActive { get; set; }
    }
}

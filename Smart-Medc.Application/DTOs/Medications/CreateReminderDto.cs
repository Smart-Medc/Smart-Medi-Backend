namespace Smart_Medc.Application.DTOs.Medications
{
    public class CreateReminderDto
    {
        public TimeOnly ReminderTime { get; set; }
        public string? DaysOfWeek { get; set; }
    }
}

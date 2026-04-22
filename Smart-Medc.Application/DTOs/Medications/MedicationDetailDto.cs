namespace Smart_Medc.Application.DTOs.Medications
{
    public class MedicationDetailDto : MedicationDto
    {
        public List<ReminderDto> Reminders { get; set; } = new();
    }
}

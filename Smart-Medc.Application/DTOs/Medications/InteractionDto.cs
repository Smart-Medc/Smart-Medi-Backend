namespace Smart_Medc.Application.DTOs.Medications
{
    public class InteractionDto
    {
        public Guid MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string? InteractionNotes { get; set; }
    }
}

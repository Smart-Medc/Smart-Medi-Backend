using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.Medications
{
    public class MedicationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public DosageRoute Route { get; set; }
        public string RouteName => Route.ToString();
        public string? Instructions { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PrescribingDoctor { get; set; }
        public MedicationStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public bool HasInteraction { get; set; }
        public string? InteractionNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReminderCount { get; set; }
        public double AdherencePercentage { get; set; }
    }
}

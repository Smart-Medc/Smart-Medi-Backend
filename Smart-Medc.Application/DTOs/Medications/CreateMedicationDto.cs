using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.Medications
{
    public class CreateMedicationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public DosageRoute Route { get; set; }
        public string? Instructions { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PrescribingDoctor { get; set; }
    }
}

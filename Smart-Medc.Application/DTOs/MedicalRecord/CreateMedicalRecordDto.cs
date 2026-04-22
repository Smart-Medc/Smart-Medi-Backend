using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class CreateMedicalRecordDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public MedicalRecordType RecordType { get; set; }
        public DateTime RecordDate { get; set; }
        public string? ProviderName { get; set; }
        public string? OrderedBy { get; set; }
        public string? FindingsSummary { get; set; }
    }
}

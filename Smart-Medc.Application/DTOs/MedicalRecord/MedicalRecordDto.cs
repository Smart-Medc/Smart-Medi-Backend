using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class MedicalRecordDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public MedicalRecordType RecordType { get; set; }
        public string RecordTypeName => RecordType.ToString();
        public DateTime RecordDate { get; set; }
        public string? ProviderName { get; set; }
        public string? OrderedBy { get; set; }
        public RecordStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? FindingsSummary { get; set; }
        public int DocumentCount { get; set; }
        public long TotalDocumentSize { get; set; }
    }
}

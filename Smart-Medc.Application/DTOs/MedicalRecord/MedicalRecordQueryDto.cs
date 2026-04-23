using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class MedicalRecordQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public MedicalRecordType? RecordType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortBy { get; set; } = "RecordDate"; // RecordDate, Title, CreatedAt
        public bool SortDescending { get; set; } = true;
    }
}

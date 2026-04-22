using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public DocumentFormat Format { get; set; }
        public string FormatName => Format.ToString();
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}

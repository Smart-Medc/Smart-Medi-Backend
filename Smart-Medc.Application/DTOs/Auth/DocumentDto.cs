namespace Smart_Medc.Application.DTOs.Auth
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string FileSizeMB => $"{FileSizeBytes / 1024.0 / 1024.0:F2} MB";
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}
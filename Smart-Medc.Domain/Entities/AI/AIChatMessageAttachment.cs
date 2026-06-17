namespace Smart_Medc.Domain.Entities.AI
{
    public class AIChatMessageAttachment
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty; // R2 path
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }

        // Navigation properties
        public AIChatMessage? Message { get; set; }
    }
}

namespace Smart_Medc.Domain.Entities.AI
{
    public class AIChatMessageAttachment
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual AIChatMessage Message { get; set; } = null!;
    }
}

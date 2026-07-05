using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.AI
{
    public enum MessageRole
    {
        User = 0,
        Assistant = 1
    }

    public class AIChatMessage
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool UsedMedicalRecords { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? TokensUsed { get; set; }

        // Soft delete tracking
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public AIChatSession? Session { get; set; }
        public ICollection<AIChatMessageAttachment> Attachments { get; set; } = new List<AIChatMessageAttachment>();
    }


}

using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.AI
{
    public class AIChatMessage
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }

        public ChatRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool UsedMedicalRecords { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual AIChatSession Session { get; set; } = null!;
        public virtual ICollection<AIChatMessageAttachment> Attachments { get; set; } = new List<AIChatMessageAttachment>();
    }


}

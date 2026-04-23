using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Domain.Entities.AI
{
    public class AIChatSession
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }

        public string? Title { get; set; }
        public bool UseMedicalRecordsContext { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMessageAt { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; } = null!;
        public virtual ICollection<AIChatMessage> Messages { get; set; } = new List<AIChatMessage>();
    }
}

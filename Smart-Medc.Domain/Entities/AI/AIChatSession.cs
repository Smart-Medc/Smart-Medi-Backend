using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Domain.Entities.AI
{
    public class AIChatSession
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string? Title { get; set; }
        public bool UseMedicalRecordsContext { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Patient? Patient { get; set; }
        public ICollection<AIChatMessage> Messages { get; set; } = new List<AIChatMessage>();
    }
}

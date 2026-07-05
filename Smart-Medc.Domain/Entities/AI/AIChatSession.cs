using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Domain.Entities.AI
{
    public class AIChatSession
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string? Title { get; set; }
        public bool UseMedicalRecordsContext { get; set; }
        // New granular flags (apply only when UseMedicalRecordsContext = true)
        public bool IncludeMedicalRecords { get; set; } = true;
        public bool IncludeCurrentMedications { get; set; } = false;
        public bool IncludePastMedications { get; set; } = false;
        public bool IncludeJournalEntries { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public Patient? Patient { get; set; }
        public ICollection<AIChatMessage> Messages { get; set; } = new List<AIChatMessage>();
    }
}

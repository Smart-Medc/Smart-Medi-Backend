namespace Smart_Medc.Domain.Entities.PatientModels
{
    public class JournalEntry
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public int? MoodLevel { get; set; } // 1-10
        public int? PainLevel { get; set; } // 1-10

        public string? Symptom { get; set; } //array of string

        public DateTime EntryDate { get; set; }
        public TimeOnly EntryTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; } = null!;
        public virtual ICollection<JournalEntryTag> Tags { get; set; } = new List<JournalEntryTag>();
    }
}

namespace Smart_Medc.Domain.Entities.PatientModels;

public class JournalEntryTag
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public string Tag { get; set; } = string.Empty;

    // Navigation
    public virtual JournalEntry JournalEntry { get; set; } = null!;
}
namespace Smart_Medc.Domain.Entities.PatientModels;

public class MedicationReminder
{
    public Guid Id { get; set; }
    public Guid MedicationId { get; set; }

    public TimeOnly ReminderTime { get; set; }
    public string? DaysOfWeek { get; set; } // JSON array: ["Monday", "Tuesday", ...] or null for daily
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual Medication Medication { get; set; } = null!;
}
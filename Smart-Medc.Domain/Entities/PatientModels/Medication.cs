using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.PatientModels;

public class Medication
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty; // e.g., "500mg"
    public string Frequency { get; set; } = string.Empty; // e.g., "Twice daily"
    public DosageRoute Route { get; set; }
    public string? Instructions { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string? PrescribingDoctor { get; set; }


    public MedicationStatus Status { get; set; } = MedicationStatus.Active;
    public bool HasInteraction { get; set; } = false;
    public string? InteractionNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public virtual Patient Patient { get; set; } = null!;
    public virtual ICollection<MedicationReminder> Reminders { get; set; } = new List<MedicationReminder>();
    public virtual ICollection<MedicationAdherenceLog> AdherenceLogs { get; set; } = new List<MedicationAdherenceLog>();
}

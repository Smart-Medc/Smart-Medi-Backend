using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.PatientModels;

public class MedicationAdherenceLog
{
    public Guid Id { get; set; }
    public Guid MedicationId { get; set; }

    public DateTime ScheduledTime { get; set; }
    public DateTime? TakenTime { get; set; }
    public AdherenceStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Medication Medication { get; set; } = null!;
}


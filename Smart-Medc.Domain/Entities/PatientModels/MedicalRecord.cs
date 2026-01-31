using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.PatientModels;

public class MedicalRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MedicalRecordType RecordType { get; set; }
    public DateTime RecordDate { get; set; }

    // Provider Info
    public string? ProviderName { get; set; } // Lab, Hospital, Doctor name
    public string? OrderedBy { get; set; } // Doctor who ordered

    public RecordStatus Status { get; set; } = RecordStatus.Final;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public string? FindingsSummary { get; set; }

    // Navigation
    public virtual Patient Patient { get; set; } = null!;
    public virtual ICollection<MedicalRecordDocument> Documents { get; set; } = new List<MedicalRecordDocument>();
    //public virtual ICollection<MedicalRecordResult> Results { get; set; } = new List<MedicalRecordResult>();
    public virtual ICollection<DataShareRecordAccess> SharedAccesses { get; set; } = new List<DataShareRecordAccess>();
}




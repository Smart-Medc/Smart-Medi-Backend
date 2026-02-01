using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Entities.AppointmentModels;
using Smart_Medc.Domain.Entities.DataSharing;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Entities.Notification;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.PatientModels;

public class Patient
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    // Personal Information
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public BloodType? BloodType { get; set; }
    public string? Address { get; set; }

    // Medical Essentials
    public string? Allergies { get; set; } // Can be JSON or comma-separated
    public bool HasNoKnownAllergies { get; set; } = false;

    // Emergency Contact
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public EmergencyContactRelationship EmergencyContactRelationship { get; set; }

    // Storage
    public long StorageUsedBytes { get; set; } = 0;
    public long StorageLimitBytes { get; set; } = 10737418240; // 10 GB default

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<Medication> Medications { get; set; } = new List<Medication>();
    public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<DataShareCode> DataShareCodes { get; set; } = new List<DataShareCode>();
    public virtual ICollection<PatientNotification> Notifications { get; set; } = new List<PatientNotification>();
    public virtual ICollection<AIChatSession> AIChatSessions { get; set; } = new List<AIChatSession>();
}




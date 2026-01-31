using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.PatientModels;

public class MedicalRecordDocument
{
    public Guid Id { get; set; }
    public Guid MedicalRecordId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // MIME type
    public DocumentFormat Format { get; set; }
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty; // Path in blob storage or file system
    public string? ThumbnailPath { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public virtual MedicalRecord MedicalRecord { get; set; } = null!;
}


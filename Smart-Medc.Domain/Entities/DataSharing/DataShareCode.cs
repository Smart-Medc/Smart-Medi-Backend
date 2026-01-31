using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.DataSharing;

public class DataShareCode
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }

    public string Code { get; set; } = string.Empty; // e.g., "MED-X7K9P"
    public string ShareUrl { get; set; } = string.Empty;

    public DataShareExpirationType ExpirationType { get; set; }
    public DateTime? ExpiresAt { get; set; } // null for permanent
    public int? MaxAccessCount { get; set; } // null for unlimited

    public DataShareStatus Status { get; set; } = DataShareStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    // Navigation
    public virtual Patient Patient { get; set; } = null!;
    public virtual ICollection<DataShareRecordAccess> RecordAccesses { get; set; } = new List<DataShareRecordAccess>();
    public virtual ICollection<DataShareAccessLog> AccessLogs { get; set; } = new List<DataShareAccessLog>();
}




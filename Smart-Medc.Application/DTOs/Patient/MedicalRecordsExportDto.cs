namespace Smart_Medc.Application.DTOs.Patient
{
    public class MedicalRecordsExportDto
    {
        public ExportMetadata Metadata { get; set; } = new();
        public PatientExportInfo? PatientInfo { get; set; }
        public List<MedicalRecordExportDto> MedicalRecords { get; set; } = new();

        public List<MedicationExportDto> Medications { get; set; } = new();

        public ExportSummary Summary { get; set; } = new();
    }

    public class ExportMetadata
    {
        public string ExportFormat { get; set; } = "json";
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public string ExportedBy { get; set; } = "Patient";
        public string Version { get; set; } = "1.0";
    }

    public class PatientExportInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? BloodType { get; set; }
        public string? Address { get; set; }
        public string? Allergies { get; set; }
        public bool HasNoKnownAllergies { get; set; }
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string EmergencyContactRelationship { get; set; } = string.Empty;
    }

    public class MedicalRecordExportDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RecordType { get; set; } = string.Empty;
        public DateTime RecordDate { get; set; }
        public string? ProviderName { get; set; }
        public string? OrderedBy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FindingsSummary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<DocumentExportDto> Documents { get; set; } = new();
    }

    public class DocumentExportDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? Base64Content { get; set; }
    }

    public class MedicationExportDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? PrescribingDoctor { get; set; }
    }

    public class ExportSummary
    {
        public int TotalMedicalRecords { get; set; }
        public int TotalDocuments { get; set; }
        public long TotalDocumentSizeBytes { get; set; }
        public int ActiveMedications { get; set; }
        public int TotalMedications { get; set; }
        public string DateRangeFrom { get; set; } = string.Empty;
        public string DateRangeTo { get; set; } = string.Empty;
    }
}
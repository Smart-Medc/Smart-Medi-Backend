
namespace Smart_Medc.Application.DTOs.Appointment
{
    public class AppointmentDetailDto : AppointmentDto
    {
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool IsRecordsShared { get; set; }
        public string? AccessCode { get; set; }
        public DateTime? AccessCodeExpiresAt { get; set; }
        public string CancellationPolicy { get; set; } = string.Empty;
        public string? PreparationInstructions { get; set; }
        public List<AppointmentHistoryItemDto> History { get; set; } = new();

        // ADDED: existing completion notes returned so the UI can
        // pre-populate the notes textarea on load.
        public string? CompletionNotes { get; set; }

        // ADDED: organization ID needed by the patient-side detail page so the
        // "View Details" button can navigate to /appointments/organization/{id}
        // without a second API call. Was previously missing from the DTO even
        // though OrganizationId exists on the Appointment entity.
        public Guid OrganizationId { get; set; }

        // Patient demographic fields
        public int? PatientAge { get; set; }
        public string? PatientGender { get; set; }
        public string? PatientEmail { get; set; }
        public string? PatientPhone { get; set; }
        public string? PatientEmergencyContact { get; set; }
        public string? PatientEmergencyContactPhone { get; set; }
        public string? PatientAllergies { get; set; }
        public bool PatientHasNoKnownAllergies { get; set; }
    }

    public class AppointmentHistoryItemDto
    {
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Reason { get; set; }
    }
}

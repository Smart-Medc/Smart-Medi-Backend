using Smart_Medc.Domain.Entities.DataSharing;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.AppointmentModels
{
    public class Appointment
    {
        public Guid Id { get; set; }
        public string AppointmentNumber { get; set; } = string.Empty; // e.g., "APT-2024-001"

        public Guid PatientId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? DoctorId { get; set; }

        public DateTime AppointmentDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int DurationMinutes { get; set; }

        public AppointmentType Type { get; set; }
        public string? ReasonForVisit { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        // Data Sharing
        public bool IsRecordsShared { get; set; } = false;
        public Guid? DataShareCodeId { get; set; }

        // Cancellation/Reschedule Info
        public int RescheduleCount { get; set; } = 0;
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public Guid? CancelledByUserId { get; set; }

        // Completion
        public DateTime? CompletedAt { get; set; }
        public string? CompletionNotes { get; set; }

        // Preparation Instructions (from organization)
        public string? PreparationInstructions { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; } = null!;
        public virtual Organization Organization { get; set; } = null!;
        public virtual Doctor? Doctor { get; set; }
        public virtual DataShareCode? DataShareCode { get; set; }
        public virtual ICollection<AppointmentReminder> Reminders { get; set; } = new List<AppointmentReminder>();
        public virtual ICollection<AppointmentStatusHistory> StatusHistory { get; set; } = new List<AppointmentStatusHistory>();
    }
}

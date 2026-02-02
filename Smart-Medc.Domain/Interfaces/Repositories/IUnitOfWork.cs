using Microsoft.EntityFrameworkCore.Storage;
using Smart_Medc.Domain.Interfaces.Repositories.AI;
using Smart_Medc.Domain.Interfaces.Repositories.Appointments;
using Smart_Medc.Domain.Interfaces.Repositories.DataSharing;
using Smart_Medc.Domain.Interfaces.Repositories.Identity;
using Smart_Medc.Domain.Interfaces.Repositories.Notifications;
using Smart_Medc.Domain.Interfaces.Repositories.Organizations;
using Smart_Medc.Domain.Interfaces.Repositories.Patients;

namespace Smart_Medc.Domain.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        // Identity & Auth
        IApplicationUserRepository Users { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IOtpVerificationRepository OtpVerifications { get; }
        IBackupCodeRepository BackupCodes { get; }
        IUserNotificationPreferenceRepository UserNotificationPreferences { get; }

        // Patient
        IPatientRepository Patients { get; }
        IMedicalRecordRepository MedicalRecords { get; }
        IMedicalRecordDocumentRepository MedicalRecordDocuments { get; }
        IMedicationRepository Medications { get; }
        IMedicationReminderRepository MedicationReminders { get; }
        IMedicationAdherenceLogRepository MedicationAdherenceLogs { get; }
        IJournalEntryRepository JournalEntries { get; }
        IJournalEntryTagRepository JournalEntryTags { get; }

        // Data Sharing
        IDataShareCodeRepository DataShareCodes { get; }
        IDataShareRecordAccessRepository DataShareRecordAccesses { get; }
        IDataShareAccessLogRepository DataShareAccessLogs { get; }

        // Organization
        IOrganizationRepository Organizations { get; }
        IOrganizationSpecializationRepository OrganizationSpecializations { get; }
        ISpecializationRepository Specializations { get; }
        IOrganizationDocumentRepository OrganizationDocuments { get; }
        IOrganizationOperatingHoursRepository OrganizationOperatingHours { get; }
        IOrganizationAvailabilitySlotRepository OrganizationAvailabilitySlots { get; }
        IOrganizationAvailabilityExceptionRepository OrganizationAvailabilityExceptions { get; }
        IConsultationFeeRepository ConsultationFees { get; }
        IOrganizationPhotoRepository OrganizationPhotos { get; }
        IDoctorRepository Doctors { get; }

        // Appointments
        IAppointmentRepository Appointments { get; }
        IAppointmentStatusHistoryRepository AppointmentStatusHistories { get; }
        IAppointmentReminderRepository AppointmentReminders { get; }

        // Notifications
        IPatientNotificationRepository PatientNotifications { get; }
        IOrganizationNotificationRepository OrganizationNotifications { get; }

        // AI Chat
        IAIChatSessionRepository AIChatSessions { get; }
        IAIChatMessageRepository AIChatMessages { get; }
        IAIChatMessageAttachmentRepository AIChatMessageAttachments { get; }

        // Unit of Work operations
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}

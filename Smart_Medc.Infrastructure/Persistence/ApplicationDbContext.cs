using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Entities.AppointmentModels;
using Smart_Medc.Domain.Entities.DataSharing;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Entities.Notification;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Identity & Auth
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
        public DbSet<BackupCode> BackupCodes => Set<BackupCode>();
        public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();

        // Patient
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
        public DbSet<MedicalRecordDocument> MedicalRecordDocuments => Set<MedicalRecordDocument>();
        public DbSet<Medication> Medications => Set<Medication>();
        public DbSet<MedicationReminder> MedicationReminders => Set<MedicationReminder>();
        public DbSet<MedicationAdherenceLog> MedicationAdherenceLogs => Set<MedicationAdherenceLog>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<JournalEntryTag> JournalEntryTags => Set<JournalEntryTag>();

        // Data Sharing
        public DbSet<DataShareCode> DataShareCodes => Set<DataShareCode>();
        public DbSet<DataShareRecordAccess> DataShareRecordAccesses => Set<DataShareRecordAccess>();
        public DbSet<DataShareAccessLog> DataShareAccessLogs => Set<DataShareAccessLog>();

        // Organization
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<OrganizationSpecialization> OrganizationSpecializations => Set<OrganizationSpecialization>();
        public DbSet<Specialization> Specializations => Set<Specialization>();
        public DbSet<OrganizationDocument> OrganizationDocuments => Set<OrganizationDocument>();
        public DbSet<OrganizationOperatingHours> OrganizationOperatingHours => Set<OrganizationOperatingHours>();
        public DbSet<OrganizationAvailabilitySlot> OrganizationAvailabilitySlots => Set<OrganizationAvailabilitySlot>();
        public DbSet<OrganizationAvailabilityException> OrganizationAvailabilityExceptions => Set<OrganizationAvailabilityException>();
        public DbSet<ConsultationFee> ConsultationFees => Set<ConsultationFee>();
        public DbSet<OrganizationPhoto> OrganizationPhotos => Set<OrganizationPhoto>();
        public DbSet<Doctor> Doctors => Set<Doctor>();

        // Appointments
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<AppointmentStatusHistory> AppointmentStatusHistories => Set<AppointmentStatusHistory>();
        public DbSet<AppointmentReminder> AppointmentReminders => Set<AppointmentReminder>();

        // Notifications
        public DbSet<PatientNotification> PatientNotifications => Set<PatientNotification>();
        public DbSet<OrganizationNotification> OrganizationNotifications => Set<OrganizationNotification>();

        // AI Chat
        public DbSet<AIChatSession> AIChatSessions => Set<AIChatSession>();
        public DbSet<AIChatMessage> AIChatMessages => Set<AIChatMessage>();
        public DbSet<AIChatMessageAttachment> AIChatMessageAttachments => Set<AIChatMessageAttachment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply all configurations from assembly
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
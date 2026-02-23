using Microsoft.EntityFrameworkCore.Storage;
using Smart_Medc.Domain.Interfaces.Repositories;
using Smart_Medc.Domain.Interfaces.Repositories.AI;
using Smart_Medc.Domain.Interfaces.Repositories.Appointments;
using Smart_Medc.Domain.Interfaces.Repositories.DataSharing;
using Smart_Medc.Domain.Interfaces.Repositories.Identity;
using Smart_Medc.Domain.Interfaces.Repositories.Notifications;
using Smart_Medc.Domain.Interfaces.Repositories.Organizations;
using Smart_Medc.Domain.Interfaces.Repositories.Patients;
using Smart_Medc.Infrastructure.Persistence.Repositories.AI;
using Smart_Medc.Infrastructure.Persistence.Repositories.Appointments;
using Smart_Medc.Infrastructure.Persistence.Repositories.DataSharing;
using Smart_Medc.Infrastructure.Persistence.Repositories.Identity;
using Smart_Medc.Infrastructure.Persistence.Repositories.Notifications;
using Smart_Medc.Infrastructure.Persistence.Repositories.Organizations;
using Smart_Medc.Infrastructure.Persistence.Repositories.Patients;

namespace Smart_Medc.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        // Identity & Auth
        private IApplicationUserRepository? _users;
        private IRefreshTokenRepository? _refreshTokens;
        private IOtpVerificationRepository? _otpVerifications;
        private IBackupCodeRepository? _backupCodes;
        private IUserNotificationPreferenceRepository? _userNotificationPreferences;

        // Patient
        private IPatientRepository? _patients;
        private IMedicalRecordRepository? _medicalRecords;
        private IMedicalRecordDocumentRepository? _medicalRecordDocuments;
        private IMedicationRepository? _medications;
        private IMedicationReminderRepository? _medicationReminders;
        private IMedicationAdherenceLogRepository? _medicationAdherenceLogs;
        private IJournalEntryRepository? _journalEntries;
        private IJournalEntryTagRepository? _journalEntryTags;

        // Data Sharing
        private IDataShareCodeRepository? _dataShareCodes;
        private IDataShareRecordAccessRepository? _dataShareRecordAccesses;
        private IDataShareAccessLogRepository? _dataShareAccessLogs;

        // Organization
        private IOrganizationRepository? _organizations;
        private IOrganizationSpecializationRepository? _organizationSpecializations;
        private ISpecializationRepository? _specializations;
        private IOrganizationDocumentRepository? _organizationDocuments;
        private IOrganizationOperatingHoursRepository? _organizationOperatingHours;
        private IOrganizationAvailabilitySlotRepository? _organizationAvailabilitySlots;
        private IOrganizationAvailabilityExceptionRepository? _organizationAvailabilityExceptions;
        private IConsultationFeeRepository? _consultationFees;
        private IOrganizationPhotoRepository? _organizationPhotos;
        private IDoctorRepository? _doctors;

        // Appointments
        private IAppointmentRepository? _appointments;
        private IAppointmentStatusHistoryRepository? _appointmentStatusHistories;
        private IAppointmentReminderRepository? _appointmentReminders;

        // Notifications
        private IPatientNotificationRepository? _patientNotifications;
        private IOrganizationNotificationRepository? _organizationNotifications;

        // AI Chat
        private IAIChatSessionRepository? _aiChatSessions;
        private IAIChatMessageRepository? _aiChatMessages;
        private IAIChatMessageAttachmentRepository? _aiChatMessageAttachments;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Identity & Auth Properties

        public IApplicationUserRepository Users =>
            _users ??= new ApplicationUserRepository(_context);

        public IRefreshTokenRepository RefreshTokens =>
            _refreshTokens ??= new RefreshTokenRepository(_context);

        public IOtpVerificationRepository OtpVerifications =>
            _otpVerifications ??= new OtpVerificationRepository(_context);

        public IBackupCodeRepository BackupCodes =>
            _backupCodes ??= new BackupCodeRepository(_context);

        public IUserNotificationPreferenceRepository UserNotificationPreferences =>
            _userNotificationPreferences ??= new UserNotificationPreferenceRepository(_context);

        #endregion

        #region Patient Properties

        public IPatientRepository Patients =>
            _patients ??= new PatientRepository(_context);

        public IMedicalRecordRepository MedicalRecords =>
            _medicalRecords ??= new MedicalRecordRepository(_context);

        public IMedicalRecordDocumentRepository MedicalRecordDocuments =>
            _medicalRecordDocuments ??= new MedicalRecordDocumentRepository(_context);

        public IMedicationRepository Medications =>
            _medications ??= new MedicationRepository(_context);

        public IMedicationReminderRepository MedicationReminders =>
            _medicationReminders ??= new MedicationReminderRepository(_context);

        public IMedicationAdherenceLogRepository MedicationAdherenceLogs =>
            _medicationAdherenceLogs ??= new MedicationAdherenceLogRepository(_context);

        public IJournalEntryRepository JournalEntries =>
            _journalEntries ??= new JournalEntryRepository(_context);

        public IJournalEntryTagRepository JournalEntryTags =>
            _journalEntryTags ??= new JournalEntryTagRepository(_context);

        #endregion

        #region Data Sharing Properties

        public IDataShareCodeRepository DataShareCodes =>
            _dataShareCodes ??= new DataShareCodeRepository(_context);

        public IDataShareRecordAccessRepository DataShareRecordAccesses =>
            _dataShareRecordAccesses ??= new DataShareRecordAccessRepository(_context);

        public IDataShareAccessLogRepository DataShareAccessLogs =>
            _dataShareAccessLogs ??= new DataShareAccessLogRepository(_context);

        #endregion

        #region Organization Properties

        public IOrganizationRepository Organizations =>
            _organizations ??= new OrganizationRepository(_context);

        public IOrganizationSpecializationRepository OrganizationSpecializations =>
            _organizationSpecializations ??= new OrganizationSpecializationRepository(_context);

        public ISpecializationRepository Specializations =>
            _specializations ??= new SpecializationRepository(_context);

        public IOrganizationDocumentRepository OrganizationDocuments =>
            _organizationDocuments ??= new OrganizationDocumentRepository(_context);

        public IOrganizationOperatingHoursRepository OrganizationOperatingHours =>
            _organizationOperatingHours ??= new OrganizationOperatingHoursRepository(_context);

        public IOrganizationAvailabilitySlotRepository OrganizationAvailabilitySlots =>
            _organizationAvailabilitySlots ??= new OrganizationAvailabilitySlotRepository(_context);

        public IOrganizationAvailabilityExceptionRepository OrganizationAvailabilityExceptions =>
            _organizationAvailabilityExceptions ??= new OrganizationAvailabilityExceptionRepository(_context);

        public IConsultationFeeRepository ConsultationFees =>
            _consultationFees ??= new ConsultationFeeRepository(_context);

        public IOrganizationPhotoRepository OrganizationPhotos =>
            _organizationPhotos ??= new OrganizationPhotoRepository(_context);

        public IDoctorRepository Doctors =>
            _doctors ??= new DoctorRepository(_context);

        #endregion

        #region Appointment Properties

        public IAppointmentRepository Appointments =>
            _appointments ??= new AppointmentRepository(_context);

        public IAppointmentStatusHistoryRepository AppointmentStatusHistories =>
            _appointmentStatusHistories ??= new AppointmentStatusHistoryRepository(_context);

        public IAppointmentReminderRepository AppointmentReminders =>
            _appointmentReminders ??= new AppointmentReminderRepository(_context);

        #endregion

        #region Notification Properties

        public IPatientNotificationRepository PatientNotifications =>
            _patientNotifications ??= new PatientNotificationRepository(_context);

        public IOrganizationNotificationRepository OrganizationNotifications =>
            _organizationNotifications ??= new OrganizationNotificationRepository(_context);

        #endregion

        #region AI Chat Properties

        public IAIChatSessionRepository AIChatSessions =>
            _aiChatSessions ??= new AIChatSessionRepository(_context);

        public IAIChatMessageRepository AIChatMessages =>
            _aiChatMessages ??= new AIChatMessageRepository(_context);

        public IAIChatMessageAttachmentRepository AIChatMessageAttachments =>
            _aiChatMessageAttachments ??= new AIChatMessageAttachmentRepository(_context);

        #endregion

        #region Unit of Work Operations

        /// <summary>
        /// Saves all pending changes to the database
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Provides execution strategy for resilient transactions
        /// </summary>
        public IExecutionStrategy CreateExecutionStrategy()
        {
            return _context.Database.CreateExecutionStrategy();
        }

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
                throw new InvalidOperationException("A transaction is already in progress.");

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
                throw new InvalidOperationException("No transaction in progress to commit.");

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
                throw new InvalidOperationException("No transaction in progress to rollback.");

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentTransaction?.Dispose();
                _context?.Dispose();
            }
        }

        #endregion
    }
}
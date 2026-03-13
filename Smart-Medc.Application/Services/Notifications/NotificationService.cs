using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.DTOs.Notifications;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Domain.Entities.Notification;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

// The Hub and its client interface live in the API layer.
// We reference IHubContext via the strongly-typed interface defined there.
// To avoid circular project references we use the non-generic IHubContext
// and the hub type passed as a string group target.
// See note at NotificationHub.cs for the two-project approach.

namespace Smart_Medc.Application.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationHubPusher _hubPusher;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork uow,
            INotificationHubPusher hubPusher,
            ILogger<NotificationService> logger)
        {
            _uow = uow;
            _hubPusher = hubPusher;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════
        //  PATIENT NOTIFICATIONS
        // ════════════════════════════════════════════════════

        public async Task<PatientNotificationDto> SendToPatientAsync(
            CreatePatientNotificationDto dto,
            CancellationToken ct = default)
        {
            // 1. Resolve the patient's ApplicationUser.Id for SignalR targeting
            var patient = await _uow.Patients
                .GetByIdWithUserAsync(dto.PatientId, ct);

            if (patient == null)
            {
                _logger.LogWarning(
                    "SendToPatient: patient {PatientId} not found", dto.PatientId);
                throw new InvalidOperationException($"Patient {dto.PatientId} not found.");
            }

            // 2. Check preferences — skip external channels if opted out
            var pref = await _uow.UserNotificationPreferences
                .GetByUserAndTypeAsync(
                    patient.UserId,
                    (int)MapPatientTypeToGeneral(dto.Type),
                    ct);

            // 3. Persist notification
            var notification = new PatientNotification
            {
                Id = Guid.NewGuid(),
                PatientId = dto.PatientId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                ActionUrl = dto.ActionUrl,
                Data = dto.Data,
                Priority = dto.Priority,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.PatientNotifications.AddAsync(notification, ct);
            await _uow.SaveChangesAsync(ct);

            // 4. Push in-app via SignalR using the patient's UserId as group key
            var resultDto = MapPatientToDto(notification);
            await _hubPusher.PushToUserAsync(
                patient.UserId.ToString(),
                "ReceiveNotification",
                resultDto,
                ct);

            // 5. Push updated unread count
            var unreadCount = await _uow.PatientNotifications
                .GetUnreadCountAsync(dto.PatientId, ct);
            await _hubPusher.PushToUserAsync(
                patient.UserId.ToString(),
                "UnreadCountUpdated",
                unreadCount,
                ct);

            _logger.LogInformation(
                "PatientNotification {Id} sent to patient {PatientId} — type={Type}",
                notification.Id, dto.PatientId, dto.Type);

            return resultDto;
        }

        public async Task<PatientNotificationListDto> GetPatientNotificationsAsync(
            Guid patientId,
            bool? isRead = null,
            PatientNotificationType? type = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            // Use the IRepository<T>.Query() for flexible filtering
            var query = _uow.PatientNotifications
                .QueryNoTracking()
                .Where(n => n.PatientId == patientId);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);

            if (type.HasValue)
                query = query.Where(n => n.Type == type.Value);

            var totalCount = await query.CountAsync(ct);
            var unreadCount = await _uow.PatientNotifications
                .GetUnreadCountAsync(patientId, ct);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PatientNotificationListDto
            {
                Items = items.Select(MapPatientToDto).ToList(),
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                Page = page,
                PageSize = pageSize,
                HasMore = (page * pageSize) < totalCount
            };
        }

        public async Task<PatientUnreadCountDto> GetPatientUnreadCountAsync(
            Guid patientId,
            CancellationToken ct = default)
        {
            var unread = await _uow.PatientNotifications
                .QueryNoTracking()
                .Where(n => n.PatientId == patientId && !n.IsRead)
                .ToListAsync(ct);

            return new PatientUnreadCountDto
            {
                TotalUnread = unread.Count,
                AppointmentReminders = unread.Count(n =>
                    n.Type == PatientNotificationType.AppointmentReminder),
                MedicationReminders = unread.Count(n =>
                    n.Type == PatientNotificationType.MedicationReminder),
                AppointmentUpdates = unread.Count(n =>
                    n.Type == PatientNotificationType.AppointmentConfirmation ||
                    n.Type == PatientNotificationType.AppointmentCancellation ||
                    n.Type == PatientNotificationType.AppointmentRequest),
                RecordAccess = unread.Count(n =>
                    n.Type == PatientNotificationType.RecordAccess),
                AIAlerts = unread.Count(n =>
                    n.Type == PatientNotificationType.AIHealthAlert),
                System = unread.Count(n =>
                    n.Type == PatientNotificationType.SystemNotification)
            };
        }

        public async Task MarkPatientNotificationAsReadAsync(
            Guid patientId,
            Guid notificationId,
            CancellationToken ct = default)
        {
            // Validate ownership before marking
            var exists = await _uow.PatientNotifications
                .AnyAsync(n => n.Id == notificationId && n.PatientId == patientId, ct);

            if (!exists)
            {
                _logger.LogWarning(
                    "MarkRead: notification {Id} not found for patient {PatientId}",
                    notificationId, patientId);
                return;
            }

            await _uow.PatientNotifications.MarkAsReadAsync(notificationId, ct);
            await _uow.SaveChangesAsync(ct);

            // Push updated count
            var patient = await _uow.Patients.GetByIdWithUserAsync(patientId, ct);
            if (patient != null)
            {
                var newCount = await _uow.PatientNotifications
                    .GetUnreadCountAsync(patientId, ct);
                await _hubPusher.PushToUserAsync(
                    patient.UserId.ToString(),
                    "UnreadCountUpdated",
                    newCount,
                    ct);
            }
        }

        public async Task MarkPatientNotificationsBatchAsReadAsync(
            Guid patientId,
            List<Guid> notificationIds,
            CancellationToken ct = default)
        {
            foreach (var id in notificationIds)
            {
                var exists = await _uow.PatientNotifications
                    .AnyAsync(n => n.Id == id && n.PatientId == patientId, ct);
                if (exists)
                    await _uow.PatientNotifications.MarkAsReadAsync(id, ct);
            }

            await _uow.SaveChangesAsync(ct);

            var patient = await _uow.Patients.GetByIdWithUserAsync(patientId, ct);
            if (patient != null)
            {
                var newCount = await _uow.PatientNotifications
                    .GetUnreadCountAsync(patientId, ct);
                await _hubPusher.PushToUserAsync(
                    patient.UserId.ToString(),
                    "UnreadCountUpdated",
                    newCount,
                    ct);
            }
        }

        public async Task MarkAllPatientNotificationsAsReadAsync(
            Guid patientId,
            CancellationToken ct = default)
        {
            await _uow.PatientNotifications.MarkAllAsReadAsync(patientId, ct);
            await _uow.SaveChangesAsync(ct);

            var patient = await _uow.Patients.GetByIdWithUserAsync(patientId, ct);
            if (patient != null)
            {
                await _hubPusher.PushToUserAsync(
                    patient.UserId.ToString(),
                    "UnreadCountUpdated",
                    0,
                    ct);
            }
        }

        // ════════════════════════════════════════════════════
        //  ORGANIZATION NOTIFICATIONS
        // ════════════════════════════════════════════════════

        public async Task<OrganizationNotificationDto> SendToOrganizationAsync(
            CreateOrganizationNotificationDto dto,
            CancellationToken ct = default)
        {
            // 1. Resolve the organization's ApplicationUser.Id
            var org = await _uow.Organizations
                .GetByIdWithDetailsAsync(dto.OrganizationId, ct);

            if (org == null)
            {
                _logger.LogWarning(
                    "SendToOrganization: org {OrgId} not found", dto.OrganizationId);
                throw new InvalidOperationException(
                    $"Organization {dto.OrganizationId} not found.");
            }

            // 2. Persist
            var notification = new OrganizationNotification
            {
                Id = Guid.NewGuid(),
                OrganizationId = dto.OrganizationId,
                Type = dto.Type,
                Title = dto.Title,
                Message = dto.Message,
                ActionUrl = dto.ActionUrl,
                Data = dto.Data,
                Priority = dto.Priority,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.OrganizationNotifications.AddAsync(notification, ct);
            await _uow.SaveChangesAsync(ct);

            // 3. Push via SignalR
            var resultDto = MapOrgToDto(notification);
            await _hubPusher.PushToUserAsync(
                org.UserId.ToString(),
                "ReceiveNotification",
                resultDto,
                ct);

            // 4. Push updated unread count
            var unreadCount = await _uow.OrganizationNotifications
                .GetUnreadCountAsync(dto.OrganizationId, ct);
            await _hubPusher.PushToUserAsync(
                org.UserId.ToString(),
                "UnreadCountUpdated",
                unreadCount,
                ct);

            _logger.LogInformation(
                "OrgNotification {Id} sent to org {OrgId} — type={Type}",
                notification.Id, dto.OrganizationId, dto.Type);

            return resultDto;
        }

        public async Task<OrganizationNotificationListDto> GetOrganizationNotificationsAsync(
            Guid organizationId,
            bool? isRead = null,
            OrganizationNotificationType? type = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            var query = _uow.OrganizationNotifications
                .QueryNoTracking()
                .Where(n => n.OrganizationId == organizationId);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);

            if (type.HasValue)
                query = query.Where(n => n.Type == type.Value);

            var totalCount = await query.CountAsync(ct);
            var unreadCount = await _uow.OrganizationNotifications
                .GetUnreadCountAsync(organizationId, ct);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new OrganizationNotificationListDto
            {
                Items = items.Select(MapOrgToDto).ToList(),
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                Page = page,
                PageSize = pageSize,
                HasMore = (page * pageSize) < totalCount
            };
        }

        public async Task<OrganizationUnreadCountDto> GetOrganizationUnreadCountAsync(
            Guid organizationId,
            CancellationToken ct = default)
        {
            var unread = await _uow.OrganizationNotifications
                .QueryNoTracking()
                .Where(n => n.OrganizationId == organizationId && !n.IsRead)
                .ToListAsync(ct);

            return new OrganizationUnreadCountDto
            {
                TotalUnread = unread.Count,
                NewAppointmentRequests = unread.Count(n =>
                    n.Type == OrganizationNotificationType.AppointmentRequest),
                AppointmentCancellations = unread.Count(n =>
                    n.Type == OrganizationNotificationType.AppointmentCancelled),
                UpcomingAppointments = unread.Count(n =>
                    n.Type == OrganizationNotificationType.AppointmentUpcoming),
                PatientNoShows = unread.Count(n =>
                    n.Type == OrganizationNotificationType.PatientNoShow),
                AppointmentRescheduled = unread.Count(n =>
                    n.Type == OrganizationNotificationType.AppointmentRescheduled),
                System = unread.Count(n =>
                    n.Type == OrganizationNotificationType.SystemNotification)
            };
        }

        public async Task MarkOrganizationNotificationAsReadAsync(
            Guid organizationId,
            Guid notificationId,
            CancellationToken ct = default)
        {
            var exists = await _uow.OrganizationNotifications
                .AnyAsync(n => n.Id == notificationId &&
                               n.OrganizationId == organizationId, ct);

            if (!exists) return;

            await _uow.OrganizationNotifications.MarkAsReadAsync(notificationId, ct);
            await _uow.SaveChangesAsync(ct);

            var org = await _uow.Organizations
                .GetByIdWithDetailsAsync(organizationId, ct);
            if (org != null)
            {
                var newCount = await _uow.OrganizationNotifications
                    .GetUnreadCountAsync(organizationId, ct);
                await _hubPusher.PushToUserAsync(
                    org.UserId.ToString(),
                    "UnreadCountUpdated",
                    newCount,
                    ct);
            }
        }

        public async Task MarkOrganizationNotificationsBatchAsReadAsync(
            Guid organizationId,
            List<Guid> notificationIds,
            CancellationToken ct = default)
        {
            foreach (var id in notificationIds)
            {
                var exists = await _uow.OrganizationNotifications
                    .AnyAsync(n => n.Id == id &&
                                   n.OrganizationId == organizationId, ct);
                if (exists)
                    await _uow.OrganizationNotifications.MarkAsReadAsync(id, ct);
            }

            await _uow.SaveChangesAsync(ct);

            var org = await _uow.Organizations
                .GetByIdWithDetailsAsync(organizationId, ct);
            if (org != null)
            {
                var newCount = await _uow.OrganizationNotifications
                    .GetUnreadCountAsync(organizationId, ct);
                await _hubPusher.PushToUserAsync(
                    org.UserId.ToString(),
                    "UnreadCountUpdated",
                    newCount,
                    ct);
            }
        }

        public async Task MarkAllOrganizationNotificationsAsReadAsync(
            Guid organizationId,
            CancellationToken ct = default)
        {
            await _uow.OrganizationNotifications
                .MarkAllAsReadAsync(organizationId, ct);
            await _uow.SaveChangesAsync(ct);

            var org = await _uow.Organizations
                .GetByIdWithDetailsAsync(organizationId, ct);
            if (org != null)
            {
                await _hubPusher.PushToUserAsync(
                    org.UserId.ToString(),
                    "UnreadCountUpdated",
                    0,
                    ct);
            }
        }

        // ════════════════════════════════════════════════════
        //  PREFERENCES
        // ════════════════════════════════════════════════════

        public async Task<List<NotificationPreferenceDto>> GetUserPreferencesAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            var saved = await _uow.UserNotificationPreferences
                .GetByUserIdAsync(userId, ct);

            var allTypes = Enum.GetValues<NotificationType>();
            var result = new List<NotificationPreferenceDto>();

            foreach (var type in allTypes)
            {
                var existing = saved.FirstOrDefault(p => p.NotificationType == type);

                result.Add(existing != null
                    ? new NotificationPreferenceDto
                    {
                        Id = existing.Id,
                        NotificationType = existing.NotificationType,
                        TypeDisplayName = GetNotificationTypeDisplayName(type),
                        EmailEnabled = existing.EmailEnabled,
                        SmsEnabled = existing.SmsEnabled,
                        PushEnabled = existing.PushEnabled
                    }
                    : new NotificationPreferenceDto
                    {
                        Id = Guid.Empty,
                        NotificationType = type,
                        TypeDisplayName = GetNotificationTypeDisplayName(type),
                        EmailEnabled = true,
                        SmsEnabled = true,
                        PushEnabled = true
                    });
            }

            return result;
        }

        public async Task UpdateUserPreferencesAsync(
            Guid userId,
            UpdateAllNotificationPreferencesDto dto,
            CancellationToken ct = default)
        {
            var existing = (await _uow.UserNotificationPreferences
                .GetByUserIdAsync(userId, ct)).ToList();

            foreach (var update in dto.Preferences)
            {
                var pref = existing.FirstOrDefault(
                    p => p.NotificationType == update.NotificationType);

                if (pref != null)
                {
                    pref.EmailEnabled = update.EmailEnabled;
                    pref.SmsEnabled = update.SmsEnabled;
                    pref.PushEnabled = update.PushEnabled;
                    pref.UpdatedAt = DateTime.UtcNow;
                    await _uow.UserNotificationPreferences.UpdateAsync(pref, ct);
                }
                else
                {
                    var newPref = new Domain.Entities.Identity.UserNotificationPreference
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        NotificationType = update.NotificationType,
                        EmailEnabled = update.EmailEnabled,
                        SmsEnabled = update.SmsEnabled,
                        PushEnabled = update.PushEnabled,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _uow.UserNotificationPreferences.AddAsync(newPref, ct);
                }
            }

            await _uow.SaveChangesAsync(ct);
        }

        // ════════════════════════════════════════════════════
        //  APPOINTMENT STATUS HELPER
        // ════════════════════════════════════════════════════

        public async Task NotifyAppointmentStatusChangedAsync(
            Guid appointmentId,
            AppointmentStatus newStatus,
            string? extraMessage = null,
            CancellationToken ct = default)
        {
            var appointment = await _uow.Appointments
                .Query()
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Organization)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

            if (appointment == null)
            {
                _logger.LogWarning(
                    "NotifyAppointmentStatusChanged: appointment {Id} not found",
                    appointmentId);
                return;
            }

            var aptNumber = appointment.AppointmentNumber;
            var orgName = appointment.Organization?.Name ?? "your provider";
            var patientName = appointment.Patient?.User?.FullName ?? "the patient";
            var aptDate = appointment.AppointmentDate.ToString("MMM dd, yyyy");
            var aptTime = appointment.StartTime.ToString("hh:mm tt");

            // ── Patient side ──────────────────────────────
            (string patientTitle, string patientMessage,
             PatientNotificationType patientType, NotificationPriority patientPriority) = newStatus switch
             {
                 AppointmentStatus.Confirmed => (
                     "Appointment Confirmed",
                     $"Your appointment with {orgName} on {aptDate} at {aptTime} has been confirmed.",
                     PatientNotificationType.AppointmentConfirmation,
                     NotificationPriority.High),

                 AppointmentStatus.Rejected => (
                     "Appointment Rejected",
                     $"Your appointment request with {orgName} on {aptDate} was rejected. {extraMessage ?? ""}".Trim(),
                     PatientNotificationType.AppointmentCancellation,
                     NotificationPriority.High),

                 AppointmentStatus.Cancelled => (
                     "Appointment Cancelled",
                     $"Your appointment with {orgName} on {aptDate} at {aptTime} has been cancelled. {extraMessage ?? ""}".Trim(),
                     PatientNotificationType.AppointmentCancellation,
                     NotificationPriority.High),

                 AppointmentStatus.Rescheduled => (
                     "Appointment Rescheduled",
                     $"Your appointment with {orgName} has been rescheduled to {aptDate} at {aptTime}.",
                     PatientNotificationType.AppointmentConfirmation,
                     NotificationPriority.Normal),

                 AppointmentStatus.Completed => (
                     "Appointment Completed",
                     $"Your appointment with {orgName} on {aptDate} has been marked as completed.",
                     PatientNotificationType.SystemNotification,
                     NotificationPriority.Low),

                 AppointmentStatus.NoShow => (
                     "Missed Appointment",
                     $"You were marked as a no-show for your appointment with {orgName} on {aptDate}.",
                     PatientNotificationType.SystemNotification,
                     NotificationPriority.Normal),

                 _ => (string.Empty, string.Empty,
                       PatientNotificationType.SystemNotification,
                       NotificationPriority.Low)
             };

            if (!string.IsNullOrEmpty(patientTitle))
            {
                await SendToPatientAsync(new CreatePatientNotificationDto
                {
                    PatientId = appointment.PatientId,
                    Type = patientType,
                    Title = patientTitle,
                    Message = patientMessage,
                    ActionUrl = $"/appointments/{appointmentId}",
                    Priority = patientPriority,
                    Data = $"{{\"appointmentId\":\"{appointmentId}\",\"appointmentNumber\":\"{aptNumber}\"}}"
                }, ct);
            }

            // ── Organization side ─────────────────────────
            (string orgTitle, string orgMessage,
             OrganizationNotificationType orgType, NotificationPriority orgPriority) = newStatus switch
             {
                 AppointmentStatus.Pending => (
                     "New Appointment Request",
                     $"{patientName} requested an appointment on {aptDate} at {aptTime}.",
                     OrganizationNotificationType.AppointmentRequest,
                     NotificationPriority.High),

                 AppointmentStatus.Cancelled => (
                     "Appointment Cancelled",
                     $"{patientName} cancelled their appointment scheduled for {aptDate} at {aptTime}. {extraMessage ?? ""}".Trim(),
                     OrganizationNotificationType.AppointmentCancelled,
                     NotificationPriority.Normal),

                 AppointmentStatus.Rescheduled => (
                     "Appointment Rescheduled",
                     $"{patientName} rescheduled their appointment to {aptDate} at {aptTime}.",
                     OrganizationNotificationType.AppointmentRescheduled,
                     NotificationPriority.Normal),

                 AppointmentStatus.NoShow => (
                     "Patient No-Show",
                     $"{patientName} did not attend their appointment on {aptDate} at {aptTime}.",
                     OrganizationNotificationType.PatientNoShow,
                     NotificationPriority.Normal),

                 _ => (string.Empty, string.Empty,
                       OrganizationNotificationType.SystemNotification,
                       NotificationPriority.Low)
             };

            if (!string.IsNullOrEmpty(orgTitle))
            {
                await SendToOrganizationAsync(new CreateOrganizationNotificationDto
                {
                    OrganizationId = appointment.OrganizationId,
                    Type = orgType,
                    Title = orgTitle,
                    Message = orgMessage,
                    ActionUrl = $"/org/appointments/{appointmentId}",
                    Priority = orgPriority,
                    Data = $"{{\"appointmentId\":\"{appointmentId}\",\"appointmentNumber\":\"{aptNumber}\"}}"
                }, ct);
            }
        }

        // ════════════════════════════════════════════════════
        //  PRIVATE MAPPERS
        // ════════════════════════════════════════════════════

        private static PatientNotificationDto MapPatientToDto(PatientNotification n) =>
            new()
            {
                Id = n.Id,
                PatientId = n.PatientId,
                Type = n.Type,
                TypeDisplayName = GetPatientTypeDisplayName(n.Type),
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                Data = n.Data,
                Priority = n.Priority,
                PriorityDisplayName = n.Priority.ToString(),
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            };

        private static OrganizationNotificationDto MapOrgToDto(OrganizationNotification n) =>
            new()
            {
                Id = n.Id,
                OrganizationId = n.OrganizationId,
                Type = n.Type,
                TypeDisplayName = GetOrgTypeDisplayName(n.Type),
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                Data = n.Data,
                Priority = n.Priority,
                PriorityDisplayName = n.Priority.ToString(),
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            };

        private static string GetPatientTypeDisplayName(PatientNotificationType type) => type switch
        {
            PatientNotificationType.AppointmentReminder => "Appointment Reminder",
            PatientNotificationType.MedicationReminder => "Medication Reminder",
            PatientNotificationType.AppointmentRequest => "Appointment Request",
            PatientNotificationType.AppointmentConfirmation => "Appointment Confirmation",
            PatientNotificationType.AppointmentCancellation => "Appointment Cancellation",
            PatientNotificationType.RecordAccess => "Record Access",
            PatientNotificationType.AIHealthAlert => "AI Health Alert",
            PatientNotificationType.SystemNotification => "System Notification",
            _ => type.ToString()
        };

        private static string GetOrgTypeDisplayName(OrganizationNotificationType type) => type switch
        {
            OrganizationNotificationType.AppointmentRequest => "New Appointment Request",
            OrganizationNotificationType.AppointmentCancelled => "Appointment Cancelled",
            OrganizationNotificationType.AppointmentUpcoming => "Upcoming Appointment",
            OrganizationNotificationType.PatientNoShow => "Patient No-Show",
            OrganizationNotificationType.AppointmentRescheduled => "Appointment Rescheduled",
            OrganizationNotificationType.SystemNotification => "System Notification",
            _ => type.ToString()
        };

        private static string GetNotificationTypeDisplayName(NotificationType type) => type switch
        {
            NotificationType.AppointmentReminder => "Appointment Reminder",
            NotificationType.MedicationReminder => "Medication Reminder",
            NotificationType.AppointmentRequest => "Appointment Request",
            NotificationType.AppointmentConfirmation => "Appointment Confirmation",
            NotificationType.AppointmentCancellation => "Appointment Cancellation",
            NotificationType.RecordAccess => "Record Access",
            NotificationType.AIHealthAlert => "AI Health Alert",
            NotificationType.SystemNotification => "System Notification",
            _ => type.ToString()
        };

        private static NotificationType MapPatientTypeToGeneral(PatientNotificationType t) => t switch
        {
            PatientNotificationType.AppointmentReminder => NotificationType.AppointmentReminder,
            PatientNotificationType.MedicationReminder => NotificationType.MedicationReminder,
            PatientNotificationType.AppointmentRequest => NotificationType.AppointmentRequest,
            PatientNotificationType.AppointmentConfirmation => NotificationType.AppointmentConfirmation,
            PatientNotificationType.AppointmentCancellation => NotificationType.AppointmentCancellation,
            PatientNotificationType.RecordAccess => NotificationType.RecordAccess,
            PatientNotificationType.AIHealthAlert => NotificationType.AIHealthAlert,
            _ => NotificationType.SystemNotification
        };
    }
}

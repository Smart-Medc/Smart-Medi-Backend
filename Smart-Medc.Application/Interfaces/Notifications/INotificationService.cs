using Smart_Medc.Application.DTOs.Notifications;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.Interfaces.Notifications
{
    public interface INotificationService
    {
        // ── Patient Notifications ─────────────────────────

        /// <summary>
        /// Creates a PatientNotification, saves it to DB,
        /// pushes it via SignalR, and optionally sends
        /// email/SMS based on the patient's preferences.
        /// </summary>
        Task<PatientNotificationDto> SendToPatientAsync(
            CreatePatientNotificationDto dto,
            CancellationToken ct = default);

        Task<PatientNotificationListDto> GetPatientNotificationsAsync(
            Guid patientId,
            bool? isRead = null,
            PatientNotificationType? type = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        Task<PatientUnreadCountDto> GetPatientUnreadCountAsync(
            Guid patientId,
            CancellationToken ct = default);

        Task MarkPatientNotificationAsReadAsync(
            Guid patientId,
            Guid notificationId,
            CancellationToken ct = default);

        Task MarkPatientNotificationsBatchAsReadAsync(
            Guid patientId,
            List<Guid> notificationIds,
            CancellationToken ct = default);

        Task MarkAllPatientNotificationsAsReadAsync(
            Guid patientId,
            CancellationToken ct = default);

        // ── Organization Notifications ────────────────────

        /// <summary>
        /// Creates an OrganizationNotification, saves it to DB,
        /// pushes it via SignalR, and optionally sends
        /// email/SMS based on preferences.
        /// </summary>
        Task<OrganizationNotificationDto> SendToOrganizationAsync(
            CreateOrganizationNotificationDto dto,
            CancellationToken ct = default);

        Task<OrganizationNotificationListDto> GetOrganizationNotificationsAsync(
            Guid organizationId,
            bool? isRead = null,
            OrganizationNotificationType? type = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        Task<OrganizationUnreadCountDto> GetOrganizationUnreadCountAsync(
            Guid organizationId,
            CancellationToken ct = default);

        Task MarkOrganizationNotificationAsReadAsync(
            Guid organizationId,
            Guid notificationId,
            CancellationToken ct = default);

        Task MarkOrganizationNotificationsBatchAsReadAsync(
            Guid organizationId,
            List<Guid> notificationIds,
            CancellationToken ct = default);

        Task MarkAllOrganizationNotificationsAsReadAsync(
            Guid organizationId,
            CancellationToken ct = default);

        // ── Preferences ───────────────────────────────────

        Task<List<NotificationPreferenceDto>> GetUserPreferencesAsync(
            Guid userId,
            CancellationToken ct = default);

        Task UpdateUserPreferencesAsync(
            Guid userId,
            UpdateAllNotificationPreferencesDto dto,
            CancellationToken ct = default);

        // ── Helpers (called by other services and jobs) ───

        /// <summary>
        /// Sends appointment-related notification to both
        /// patient and organization based on the status change.
        /// Called by AppointmentService on every status transition.
        /// </summary>
        Task NotifyAppointmentStatusChangedAsync(
            Guid appointmentId,
            AppointmentStatus newStatus,
            string? extraMessage = null,
            CancellationToken ct = default);
    }
}

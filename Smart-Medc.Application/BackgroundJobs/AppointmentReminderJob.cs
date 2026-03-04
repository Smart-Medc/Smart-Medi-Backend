using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.DTOs.Notifications;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.BackgroundJobs
{
    /// <summary>
    /// Scans for confirmed appointments coming up in the next 24 hours
    /// or next 1 hour and sends reminder notifications.
    /// Runs every 30 minutes via Hangfire recurring job.
    /// </summary>
    public class AppointmentReminderJob
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AppointmentReminderJob> _logger;

        public AppointmentReminderJob(
            IUnitOfWork uow,
            INotificationService notificationService,
            ILogger<AppointmentReminderJob> logger)
        {
            _uow = uow;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // ── 24-hour window ───────────────────────────
            var window24Start = now.AddHours(23);
            var window24End = now.AddHours(25);

            // ── 1-hour window ────────────────────────────
            var window1Start = now.AddMinutes(50);
            var window1End = now.AddMinutes(70);

            var upcoming = await _uow.Appointments
                .Query()
                .Where(a =>
                    a.Status == AppointmentStatus.Confirmed &&
                    (
                        (a.AppointmentDate >= window24Start && a.AppointmentDate <= window24End) ||
                        (a.AppointmentDate >= window1Start && a.AppointmentDate <= window1End)
                    ))
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Organization)
                .AsNoTracking()
                .ToListAsync(ct);

            _logger.LogInformation(
                "AppointmentReminderJob: found {Count} appointments due for reminders", upcoming.Count);

            foreach (var apt in upcoming)
            {
                var timeUntil = apt.AppointmentDate - now;
                bool is24hReminder = timeUntil.TotalHours >= 22 && timeUntil.TotalHours <= 26;
                bool is1hReminder = timeUntil.TotalMinutes >= 45 && timeUntil.TotalMinutes <= 75;

                var timeLabel = is24hReminder ? "tomorrow" : "in 1 hour";
                var aptDate = apt.AppointmentDate.ToString("MMM dd, yyyy");
                var aptTime = apt.StartTime.ToString("hh:mm tt");
                var orgName = apt.Organization?.Name ?? "your provider";
                var patientName = apt.Patient?.User?.FullName ?? "the patient";
                var aptId = apt.Id.ToString();
                var aptNumber = apt.AppointmentNumber;

                // Check if we already sent this specific reminder today
                // We use the Data JSON field to store appointment context
                // and check by PatientId + Type + Data to avoid duplicates.
                var alreadySentPatient = await _uow.PatientNotifications
                    .AnyAsync(n =>
                        n.PatientId == apt.PatientId &&
                        n.Type == PatientNotificationType.AppointmentReminder &&
                        n.Data != null &&
                        n.Data.Contains(aptId) &&
                        n.CreatedAt >= DateTime.UtcNow.Date, ct);

                if (!alreadySentPatient)
                {
                    await _notificationService.SendToPatientAsync(
                        new CreatePatientNotificationDto
                        {
                            PatientId = apt.PatientId,
                            Type = PatientNotificationType.AppointmentReminder,
                            Priority = is1hReminder
                                ? NotificationPriority.High
                                : NotificationPriority.Normal,
                            Title = $"Appointment Reminder — {timeLabel}",
                            Message = $"You have an appointment with {orgName} {timeLabel} on {aptDate} at {aptTime}.",
                            ActionUrl = $"/appointments/{apt.Id}",
                            Data = $"{{\"appointmentId\":\"{aptId}\",\"appointmentNumber\":\"{aptNumber}\",\"reminderType\":\"{(is24hReminder ? "24h" : "1h")}\"}}"
                        }, ct);
                }

                // Organization — 1-hour reminder only
                if (is1hReminder)
                {
                    var alreadySentOrg = await _uow.OrganizationNotifications
                        .AnyAsync(n =>
                            n.OrganizationId == apt.OrganizationId &&
                            n.Type == OrganizationNotificationType.AppointmentUpcoming &&
                            n.Data != null &&
                            n.Data.Contains(aptId) &&
                            n.CreatedAt >= DateTime.UtcNow.Date, ct);

                    if (!alreadySentOrg)
                    {
                        await _notificationService.SendToOrganizationAsync(
                            new CreateOrganizationNotificationDto
                            {
                                OrganizationId = apt.OrganizationId,
                                Type = OrganizationNotificationType.AppointmentUpcoming,
                                Priority = NotificationPriority.Normal,
                                Title = "Upcoming Appointment in 1 Hour",
                                Message = $"Appointment with {patientName} is in 1 hour at {aptTime} ({apt.AppointmentNumber}).",
                                ActionUrl = $"/org/appointments/{apt.Id}",
                                Data = $"{{\"appointmentId\":\"{aptId}\",\"appointmentNumber\":\"{aptNumber}\"}}"
                            }, ct);
                    }
                }

                _logger.LogDebug(
                    "Reminder sent for appointment {AptNumber} ({ReminderType})",
                    apt.AppointmentNumber, is24hReminder ? "24h" : "1h");
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.DTOs.Notifications;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.BackgroundJobs
{
    /// <summary>
    /// Checks all active medication reminders every 5 minutes.
    /// Sends a push notification if the current time falls within
    /// ±3 minutes of the scheduled reminder time and it hasn't
    /// already been sent today.
    /// </summary>
    public class MedicationReminderJob
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MedicationReminderJob> _logger;

        public MedicationReminderJob(
            IUnitOfWork uow,
            INotificationService notificationService,
            ILogger<MedicationReminderJob> logger)
        {
            _uow = uow;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var currentTime = TimeOnly.FromDateTime(now);
            var todayStart = DateTime.UtcNow.Date;

            // Load all active reminders with their medication and patient
            var reminders = await _uow.MedicationReminders
                .Query()
                .Where(r => r.IsActive)
                .Include(r => r.Medication)
                    .ThenInclude(m => m.Patient)
                        .ThenInclude(p => p.User)
                .Where(r =>
                    r.Medication != null &&
                    r.Medication.Status == MedicationStatus.Active &&
                    !r.Medication.IsDeleted)
                .AsNoTracking()
                .ToListAsync(ct);

            _logger.LogDebug(
                "MedicationReminderJob: checking {Count} active reminders", reminders.Count);

            foreach (var reminder in reminders)
            {
                // Only trigger if within ±3 minutes of scheduled time
                var diff = Math.Abs((currentTime - reminder.ReminderTime).TotalMinutes);
                if (diff > 3) continue;

                // Check DaysOfWeek restriction
                if (!string.IsNullOrEmpty(reminder.DaysOfWeek))
                {
                    var allowedDays = System.Text.Json.JsonSerializer
                        .Deserialize<List<string>>(reminder.DaysOfWeek) ?? new List<string>();
                    var todayName = now.DayOfWeek.ToString();
                    if (!allowedDays.Contains(todayName, StringComparer.OrdinalIgnoreCase))
                        continue;
                }

                var med = reminder.Medication;
                if (med?.Patient == null) continue;

                // Dedup: only one per medication per day
                var alreadySent = await _uow.PatientNotifications
                    .AnyAsync(n =>
                        n.PatientId == med.PatientId &&
                        n.Type == PatientNotificationType.MedicationReminder &&
                        n.Data != null &&
                        n.Data.Contains(med.Id.ToString()) &&
                        n.Data.Contains(reminder.ReminderTime.ToString("HH:mm")) &&
                        n.CreatedAt >= todayStart, ct);

                if (alreadySent) continue;

                await _notificationService.SendToPatientAsync(
                    new CreatePatientNotificationDto
                    {
                        PatientId = med.PatientId,
                        Type = PatientNotificationType.MedicationReminder,
                        Priority = NotificationPriority.High,
                        Title = "Medication Reminder",
                        Message = $"Time to take {med.Name} {med.Dosage}",
                        ActionUrl = "/medications",
                        Data = $"{{\"medicationId\":\"{med.Id}\",\"medicationName\":\"{med.Name}\",\"dosage\":\"{med.Dosage}\",\"reminderTime\":\"{reminder.ReminderTime:HH:mm}\"}}"
                    }, ct);

                _logger.LogInformation(
                    "Medication reminder sent for {MedName} to patient {PatientId}",
                    med.Name, med.PatientId);
            }
        }
    }
}

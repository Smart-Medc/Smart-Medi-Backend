using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.DTOs.Notifications;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.BackgroundJobs
{
    /// <summary>
    /// Automatically rejects appointment requests that have been
    /// pending for more than the organization's AutoRejectDays setting.
    /// Runs every hour via Hangfire.
    /// </summary>
    public class AutoRejectAppointmentJob
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AutoRejectAppointmentJob> _logger;

        public AutoRejectAppointmentJob(
            IUnitOfWork uow,
            INotificationService notificationService,
            ILogger<AutoRejectAppointmentJob> logger)
        {
            _uow = uow;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ProcessAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // Load all pending appointments with organization settings
            var pending = await _uow.Appointments
                .Query()
                .Where(a => a.Status == AppointmentStatus.Pending)
                .Include(a => a.Organization)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var apt in pending)
            {
                var autoRejectDays = apt.Organization?.AutoRejectDays ?? 3;
                var deadline = apt.CreatedAt.AddDays(autoRejectDays);

                if (now < deadline) continue;

                // Update status to Rejected
                var toUpdate = await _uow.Appointments
                    .GetByIdAsync(apt.Id, ct);
                if (toUpdate == null) continue;

                toUpdate.Status = AppointmentStatus.Rejected;
                toUpdate.CancellationReason = "Auto-rejected: no response within the allowed review period.";
                toUpdate.CancelledAt = now;
                toUpdate.UpdatedAt = now;

                await _uow.Appointments.UpdateAsync(toUpdate, ct);

                // Add status history
                await _uow.AppointmentStatusHistories.AddAsync(
                    new Domain.Entities.AppointmentModels.AppointmentStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        AppointmentId = apt.Id,
                        FromStatus = AppointmentStatus.Pending,
                        ToStatus = AppointmentStatus.Rejected,
                        Reason = "Auto-rejected by system",
                        CreatedAt = now
                    }, ct);

                await _uow.SaveChangesAsync(ct);

                // Notify patient
                await _notificationService.SendToPatientAsync(
                    new CreatePatientNotificationDto
                    {
                        PatientId = apt.PatientId,
                        Type = PatientNotificationType.AppointmentCancellation,
                        Priority = NotificationPriority.Normal,
                        Title = "Appointment Request Not Accepted",
                        Message = $"Your appointment request with {apt.Organization?.Name ?? "the provider"} was automatically rejected as no response was received within {autoRejectDays} days.",
                        ActionUrl = $"/appointments/{apt.Id}",
                        Data = $"{{\"appointmentId\":\"{apt.Id}\",\"appointmentNumber\":\"{apt.AppointmentNumber}\"}}"
                    }, ct);

                _logger.LogInformation(
                    "Auto-rejected appointment {AptNumber} (created {CreatedAt})",
                    apt.AppointmentNumber, apt.CreatedAt);
            }
        }
    }
}

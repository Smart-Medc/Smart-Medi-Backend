using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.BackgroundJobs
{
    /// <summary>
    /// Deletes read notifications older than 90 days.
    /// Runs daily at 3 AM UTC.
    /// </summary>
    public class NotificationCleanupJob
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<NotificationCleanupJob> _logger;

        public NotificationCleanupJob(
            IUnitOfWork uow,
            ILogger<NotificationCleanupJob> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task ProcessAsync(CancellationToken ct = default)
        {
            var cutoff = DateTime.UtcNow.AddDays(-90);

            // Delete old read patient notifications
            var oldPatient = await _uow.PatientNotifications
                .Query()
                .Where(n => n.IsRead && n.CreatedAt < cutoff)
                .ToListAsync(ct);

            if (oldPatient.Any())
            {
                await _uow.PatientNotifications.DeleteRangeAsync(oldPatient, ct);
                _logger.LogInformation(
                    "Cleanup: deleted {Count} old patient notifications", oldPatient.Count);
            }

            // Delete old read organization notifications
            var oldOrg = await _uow.OrganizationNotifications
                .Query()
                .Where(n => n.IsRead && n.CreatedAt < cutoff)
                .ToListAsync(ct);

            if (oldOrg.Any())
            {
                await _uow.OrganizationNotifications.DeleteRangeAsync(oldOrg, ct);
                _logger.LogInformation(
                    "Cleanup: deleted {Count} old organization notifications", oldOrg.Count);
            }

            await _uow.SaveChangesAsync(ct);
        }
    }
}

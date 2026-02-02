using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.Notification;
using Smart_Medc.Domain.Interfaces.Repositories.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Notifications
{
    public class PatientNotificationRepository : Repository<PatientNotification>, IPatientNotificationRepository
    {
        public PatientNotificationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<PatientNotification>> GetByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.PatientId == patientId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<PatientNotification>> GetUnreadByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.PatientId == patientId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(n => n.PatientId == patientId && !n.IsRead, cancellationToken);
        }

        public async Task MarkAsReadAsync(
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            var notification = await _dbSet.FindAsync(new object[] { notificationId }, cancellationToken);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                _dbSet.Update(notification);
            }
        }

        public async Task MarkAllAsReadAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            var notifications = await _dbSet
                .Where(n => n.PatientId == patientId && !n.IsRead)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            _dbSet.UpdateRange(notifications);
        }
    }
}

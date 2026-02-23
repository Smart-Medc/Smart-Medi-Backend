using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.Notification;
using Smart_Medc.Domain.Interfaces.Repositories.Notifications;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Notifications
{
    public class OrganizationNotificationRepository : Repository<OrganizationNotification>, IOrganizationNotificationRepository
    {
        public OrganizationNotificationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<OrganizationNotification>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.OrganizationId == organizationId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrganizationNotification>> GetUnreadByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.OrganizationId == organizationId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(n => n.OrganizationId == organizationId && !n.IsRead, cancellationToken);
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
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var notifications = await _dbSet
                .Where(n => n.OrganizationId == organizationId && !n.IsRead)
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

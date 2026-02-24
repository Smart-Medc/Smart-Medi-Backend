using Smart_Medc.Domain.Entities.Notification;

namespace Smart_Medc.Domain.Interfaces.Repositories.Notifications
{
    public interface IOrganizationNotificationRepository : IRepository<OrganizationNotification>
    {
        Task<IEnumerable<OrganizationNotification>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrganizationNotification>> GetUnreadByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}

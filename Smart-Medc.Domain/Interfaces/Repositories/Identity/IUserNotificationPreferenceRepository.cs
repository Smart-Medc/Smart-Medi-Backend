using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Domain.Interfaces.Repositories.Identity
{
    public interface IUserNotificationPreferenceRepository : IRepository<UserNotificationPreference>
    {
        Task<IEnumerable<UserNotificationPreference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<UserNotificationPreference?> GetByUserAndTypeAsync(Guid userId, int notificationType, CancellationToken cancellationToken = default);
    }
}

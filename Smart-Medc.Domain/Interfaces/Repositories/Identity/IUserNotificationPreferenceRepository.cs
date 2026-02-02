using Smart_Medc.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Identity
{
    public interface IUserNotificationPreferenceRepository : IRepository<UserNotificationPreference>
    {
        Task<IEnumerable<UserNotificationPreference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<UserNotificationPreference?> GetByUserAndTypeAsync(Guid userId, int notificationType, CancellationToken cancellationToken = default);
    }
}

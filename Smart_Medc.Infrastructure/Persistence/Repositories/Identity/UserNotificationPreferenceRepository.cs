using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Interfaces.Repositories.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Identity
{
    public class UserNotificationPreferenceRepository : Repository<UserNotificationPreference>, IUserNotificationPreferenceRepository
    {
        public UserNotificationPreferenceRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<UserNotificationPreference>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.NotificationType)
                .ToListAsync(cancellationToken);
        }

        public async Task<UserNotificationPreference?> GetByUserAndTypeAsync(
            Guid userId,
            int notificationType,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.UserId == userId &&
                                         (int)p.NotificationType == notificationType,
                                    cancellationToken);
        }
    }
}

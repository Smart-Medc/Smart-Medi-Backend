using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.DataSharing;
using Smart_Medc.Domain.Interfaces.Repositories.DataSharing;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.DataSharing
{
    public class DataShareAccessLogRepository : Repository<DataShareAccessLog>, IDataShareAccessLogRepository
    {
        public DataShareAccessLogRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<DataShareAccessLog>> GetByDataShareCodeIdAsync(
            Guid dataShareCodeId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.DataShareCodeId == dataShareCodeId)
                .Include(l => l.Organization)
                .OrderByDescending(l => l.AccessedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<DataShareAccessLog>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.OrganizationId == organizationId)
                .Include(l => l.DataShareCode)
                    .ThenInclude(d => d.Patient)
                        // FIX: was missing this level — Patient.User was never
                        // loaded so FullName always resolved to null, causing
                        // the service to fall back to "Unknown Patient".
                        .ThenInclude(p => p.User)
                .OrderByDescending(l => l.AccessedAt)
                .ToListAsync(cancellationToken);
        }
    }
}

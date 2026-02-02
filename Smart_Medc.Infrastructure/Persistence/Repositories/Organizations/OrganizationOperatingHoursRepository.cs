using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Interfaces.Repositories.Organizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Organizations
{
    public class OrganizationOperatingHoursRepository : Repository<OrganizationOperatingHours>, IOrganizationOperatingHoursRepository
    {
        public OrganizationOperatingHoursRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<OrganizationOperatingHours>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(oh => oh.OrganizationId == organizationId)
                .OrderBy(oh => oh.DayOfWeek)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrganizationOperatingHours?> GetByDayAsync(
            Guid organizationId,
            int dayOfWeek,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(oh => oh.OrganizationId == organizationId &&
                                          (int)oh.DayOfWeek == dayOfWeek,
                                    cancellationToken);
        }
    }
}

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
    public class OrganizationAvailabilityExceptionRepository : Repository<OrganizationAvailabilityException>, IOrganizationAvailabilityExceptionRepository
    {
        public OrganizationAvailabilityExceptionRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<OrganizationAvailabilityException>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => e.OrganizationId == organizationId)
                .OrderBy(e => e.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrganizationAvailabilityException?> GetByDateAsync(
            Guid organizationId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(e => e.OrganizationId == organizationId &&
                                         e.Date.Date == date.Date,
                                    cancellationToken);
        }

        public async Task<IEnumerable<OrganizationAvailabilityException>> GetByDateRangeAsync(
            Guid organizationId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => e.OrganizationId == organizationId &&
                           e.Date >= startDate &&
                           e.Date <= endDate)
                .OrderBy(e => e.Date)
                .ToListAsync(cancellationToken);
        }
    }
}
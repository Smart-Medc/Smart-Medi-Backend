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
    public class OrganizationAvailabilitySlotRepository : Repository<OrganizationAvailabilitySlot>, IOrganizationAvailabilitySlotRepository
    {
        public OrganizationAvailabilitySlotRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<OrganizationAvailabilitySlot>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(s => s.OrganizationId == organizationId)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrganizationAvailabilitySlot>> GetByDayAsync(
            Guid organizationId,
            int dayOfWeek,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(s => s.OrganizationId == organizationId && (int)s.DayOfWeek == dayOfWeek)
                .OrderBy(s => s.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrganizationAvailabilitySlot>> GetEnabledSlotsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(s => s.OrganizationId == organizationId && s.IsEnabled)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync(cancellationToken);
        }
    }
}

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
    public class OrganizationSpecializationRepository : Repository<OrganizationSpecialization>, IOrganizationSpecializationRepository
    {
        public OrganizationSpecializationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<OrganizationSpecialization>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(os => os.Specialization)
                .Where(os => os.OrganizationId == organizationId)
                .OrderByDescending(os => os.IsPrimary)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrganizationSpecialization?> GetPrimarySpecializationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(os => os.Specialization)
                .FirstOrDefaultAsync(os => os.OrganizationId == organizationId &&
                                          os.IsPrimary,
                                    cancellationToken);
        }
    }
}
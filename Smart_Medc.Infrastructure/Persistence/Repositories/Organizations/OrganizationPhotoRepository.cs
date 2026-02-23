using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Interfaces.Repositories.Organizations;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Organizations
{
    public class OrganizationPhotoRepository : Repository<OrganizationPhoto>, IOrganizationPhotoRepository
    {
        public OrganizationPhotoRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<OrganizationPhoto>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.OrganizationId == organizationId)
                .OrderBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.IsFeatured)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrganizationPhoto?> GetFeaturedPhotoAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.OrganizationId == organizationId &&
                                         p.IsFeatured,
                                    cancellationToken);
        }
    }
}
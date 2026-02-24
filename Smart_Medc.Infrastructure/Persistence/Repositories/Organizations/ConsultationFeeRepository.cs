using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Interfaces.Repositories.Organizations;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Organizations
{
    public class ConsultationFeeRepository : Repository<ConsultationFee>, IConsultationFeeRepository
    {
        public ConsultationFeeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<ConsultationFee>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(cf => cf.OrganizationId == organizationId)
                .OrderBy(cf => cf.FeeType)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ConsultationFee>> GetActiveFeesAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(cf => cf.OrganizationId == organizationId && cf.IsActive)
                .OrderBy(cf => cf.MinAmount)
                .ToListAsync(cancellationToken);
        }
    }
}
using Smart_Medc.Domain.Entities.OrganizationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationAvailabilityExceptionRepository : IRepository<OrganizationAvailabilityException>
    {
        Task<IEnumerable<OrganizationAvailabilityException>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<OrganizationAvailabilityException?> GetByDateAsync(Guid organizationId, DateTime date, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrganizationAvailabilityException>> GetByDateRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}

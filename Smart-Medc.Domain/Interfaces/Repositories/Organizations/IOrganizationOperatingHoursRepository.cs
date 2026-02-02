using Smart_Medc.Domain.Entities.OrganizationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationOperatingHoursRepository : IRepository<OrganizationOperatingHours>
    {
        Task<IEnumerable<OrganizationOperatingHours>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<OrganizationOperatingHours?> GetByDayAsync(Guid organizationId, int dayOfWeek, CancellationToken cancellationToken = default);
    }
}

using Smart_Medc.Domain.Entities.OrganizationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationAvailabilitySlotRepository : IRepository<OrganizationAvailabilitySlot>
    {
        Task<IEnumerable<OrganizationAvailabilitySlot>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrganizationAvailabilitySlot>> GetByDayAsync(Guid organizationId, int dayOfWeek, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrganizationAvailabilitySlot>> GetEnabledSlotsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}

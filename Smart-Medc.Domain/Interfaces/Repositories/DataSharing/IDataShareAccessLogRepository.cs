using Smart_Medc.Domain.Entities.DataSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.DataSharing
{
    public interface IDataShareAccessLogRepository : IRepository<DataShareAccessLog>
    {
        Task<IEnumerable<DataShareAccessLog>> GetByDataShareCodeIdAsync(Guid dataShareCodeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DataShareAccessLog>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}

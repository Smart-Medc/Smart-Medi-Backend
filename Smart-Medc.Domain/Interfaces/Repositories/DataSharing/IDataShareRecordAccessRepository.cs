using Smart_Medc.Domain.Entities.DataSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.DataSharing
{
    public interface IDataShareRecordAccessRepository : IRepository<DataShareRecordAccess>
    {
        Task<IEnumerable<DataShareRecordAccess>> GetByDataShareCodeIdAsync(Guid dataShareCodeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DataShareRecordAccess>> GetByMedicalRecordIdAsync(Guid medicalRecordId, CancellationToken cancellationToken = default);
    }
}

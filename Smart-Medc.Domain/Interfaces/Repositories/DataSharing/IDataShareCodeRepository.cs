using Smart_Medc.Domain.Entities.DataSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.DataSharing
{
    public interface IDataShareCodeRepository : IRepository<DataShareCode>
    {
        Task<DataShareCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<DataShareCode?> GetByCodeWithRecordsAsync(string code, CancellationToken cancellationToken = default);
        Task<IEnumerable<DataShareCode>> GetActiveCodesByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DataShareCode>> GetExpiredCodesAsync(CancellationToken cancellationToken = default);
        Task<bool> IsCodeUniqueAsync(string code, CancellationToken cancellationToken = default);
    }
}

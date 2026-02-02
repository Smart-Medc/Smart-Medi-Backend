using Smart_Medc.Domain.Entities.PatientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Patients
{
    public interface IMedicationAdherenceLogRepository : IRepository<MedicationAdherenceLog>
    {
        Task<IEnumerable<MedicationAdherenceLog>> GetByMedicationIdAsync(Guid medicationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<MedicationAdherenceLog>> GetByDateRangeAsync(Guid medicationId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<double> GetAdherenceRateAsync(Guid medicationId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}

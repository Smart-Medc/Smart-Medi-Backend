using Smart_Medc.Domain.Entities.PatientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Patients
{
    public interface IMedicalRecordRepository : IRepository<MedicalRecord>
    {
        Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(Guid patientId, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<MedicalRecord?> GetByIdWithDocumentsAsync(Guid recordId, CancellationToken cancellationToken = default);
        Task<IEnumerable<MedicalRecord>> GetByPatientIdAndTypeAsync(Guid patientId, int recordType, CancellationToken cancellationToken = default);
        Task<IEnumerable<MedicalRecord>> GetByDateRangeAsync(Guid patientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<MedicalRecord>> GetSharedRecordsAsync(Guid patientId, Guid dataShareCodeId, CancellationToken cancellationToken = default);
    }
}

using Smart_Medc.Domain.Entities.PatientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Patients
{
    public interface IMedicalRecordDocumentRepository : IRepository<MedicalRecordDocument>
    {
        Task<IEnumerable<MedicalRecordDocument>> GetByMedicalRecordIdAsync(Guid medicalRecordId, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<long> GetTotalSizeByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    }
}

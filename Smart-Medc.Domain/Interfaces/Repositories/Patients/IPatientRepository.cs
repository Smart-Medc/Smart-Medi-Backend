using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Patients
{
    public interface IPatientRepository : IRepository<Patient>
    {
        Task<Patient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Patient?> GetByIdWithUserAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<Patient?> GetByIdWithMedicalRecordsAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<bool> UpdateStorageUsedAsync(Guid patientId, long bytes, CancellationToken cancellationToken = default);
        Task<long> GetStorageUsedAsync(Guid patientId, CancellationToken cancellationToken = default);
    }
}

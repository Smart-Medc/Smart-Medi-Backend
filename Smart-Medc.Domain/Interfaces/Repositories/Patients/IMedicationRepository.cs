using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Patients
{
    public interface IMedicationRepository : IRepository<Medication>
    {
        Task<IEnumerable<Medication>> GetByPatientIdAsync(Guid patientId, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<IEnumerable<Medication>> GetActiveMedicationsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<Medication?> GetByIdWithRemindersAsync(Guid medicationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Medication>> GetMedicationsWithInteractionsAsync(Guid patientId, CancellationToken cancellationToken = default);
    }
}

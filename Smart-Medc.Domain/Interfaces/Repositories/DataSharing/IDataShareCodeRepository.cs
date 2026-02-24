using Smart_Medc.Domain.Entities.DataSharing;

namespace Smart_Medc.Domain.Interfaces.Repositories.DataSharing
{
    public interface IDataShareCodeRepository : IRepository<DataShareCode>
    {
        Task<DataShareCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<DataShareCode?> GetByCodeWithRecordsAsync(string code, CancellationToken cancellationToken = default);
        Task<IEnumerable<DataShareCode>> GetCodesByPatientIdAsync(Guid patientId, bool activeOnly = true, CancellationToken cancellationToken = default);
        Task<IEnumerable<DataShareCode>> GetExpiredCodesAsync(CancellationToken cancellationToken = default);
        Task<bool> IsCodeUniqueAsync(string code, CancellationToken cancellationToken = default);
    }
}

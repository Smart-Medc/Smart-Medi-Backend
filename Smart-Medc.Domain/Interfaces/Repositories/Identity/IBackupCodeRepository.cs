using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Domain.Interfaces.Repositories.Identity
{
    public interface IBackupCodeRepository : IRepository<BackupCode>
    {
        Task<BackupCode?> GetUnusedCodeAsync(string code, Guid userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<BackupCode>> GetUnusedCodesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetUnusedCodesCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task DeleteAllUserCodesAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}

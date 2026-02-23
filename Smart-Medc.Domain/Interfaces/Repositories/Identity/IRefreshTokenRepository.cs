using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Domain.Interfaces.Repositories.Identity
{
    public interface IRefreshTokenRepository : IRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
        Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
    }
}

using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Domain.Interfaces.Repositories.Identity
{
    public interface IOtpVerificationRepository : IRepository<OtpVerification>
    {
        Task<OtpVerification?> GetValidOtpAsync(string code, int purpose, Guid userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OtpVerification>> GetActiveOtpsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task DeleteExpiredOtpsAsync(CancellationToken cancellationToken = default);
        Task InvalidateOtpsForUserAndPurposeAsync(Guid userId, int purpose, CancellationToken cancellationToken = default);
    }
}

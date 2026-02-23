using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Interfaces.Repositories.Identity;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Identity
{
    public class OtpVerificationRepository : Repository<OtpVerification>, IOtpVerificationRepository
    {
        public OtpVerificationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<OtpVerification?> GetValidOtpAsync(
            string code,
            int purpose,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .FirstOrDefaultAsync(o => o.Code == code &&
                                         (int)o.Purpose == purpose &&
                                         o.UserId == userId &&
                                         !o.IsUsed &&
                                         o.ExpiresAt > now,
                                    cancellationToken);
        }

        public async Task<IEnumerable<OtpVerification>> GetActiveOtpsByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(o => o.UserId == userId &&
                           !o.IsUsed &&
                           o.ExpiresAt > now)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task DeleteExpiredOtpsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expiredOtps = await _dbSet
                .Where(o => o.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            _dbSet.RemoveRange(expiredOtps);
        }

        public async Task InvalidateOtpsForUserAndPurposeAsync(
            Guid userId,
            int purpose,
            CancellationToken cancellationToken = default)
        {
            var otps = await _dbSet
                .Where(o => o.UserId == userId &&
                           (int)o.Purpose == purpose &&
                           !o.IsUsed)
                .ToListAsync(cancellationToken);

            foreach (var otp in otps)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            _dbSet.UpdateRange(otps);
        }
    }
}

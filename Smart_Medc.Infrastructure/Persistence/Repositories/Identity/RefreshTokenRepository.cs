using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Interfaces.Repositories.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Identity
{
    public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(ApplicationDbContext context) : base(context) { }

        public async Task<RefreshToken?> GetByTokenAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        }

        public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(rt => rt.UserId == userId &&
                            rt.ExpiresAt > now &&
                            rt.RevokedAt == null)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task RevokeAllUserTokensAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var tokens = await _dbSet
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.RevokedAt = now;
                token.ReasonRevoked = "All tokens revoked by user";
            }

            _dbSet.UpdateRange(tokens);
        }

        public async Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expiredTokens = await _dbSet
                .Where(rt => rt.ExpiresAt < now)
                .ToListAsync(cancellationToken);

            _dbSet.RemoveRange(expiredTokens);
        }
    }
}

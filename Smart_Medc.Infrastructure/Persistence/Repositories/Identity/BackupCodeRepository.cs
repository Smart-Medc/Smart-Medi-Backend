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
    public class BackupCodeRepository : Repository<BackupCode>, IBackupCodeRepository
    {
        public BackupCodeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<BackupCode?> GetUnusedCodeAsync(
            string code,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(bc => bc.Code == code &&
                                          bc.UserId == userId &&
                                          !bc.IsUsed,
                                    cancellationToken);
        }

        public async Task<IEnumerable<BackupCode>> GetUnusedCodesByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(bc => bc.UserId == userId && !bc.IsUsed)
                .OrderBy(bc => bc.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnusedCodesCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(bc => bc.UserId == userId && !bc.IsUsed, cancellationToken);
        }

        public async Task DeleteAllUserCodesAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var codes = await _dbSet
                .Where(bc => bc.UserId == userId)
                .ToListAsync(cancellationToken);

            _dbSet.RemoveRange(codes);
        }
    }
}

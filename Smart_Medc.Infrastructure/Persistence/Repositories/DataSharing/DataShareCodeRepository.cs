using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.DataSharing;
using Smart_Medc.Domain.Interfaces.Repositories.DataSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.DataSharing
{
    public class DataShareCodeRepository : Repository<DataShareCode>, IDataShareCodeRepository
    {
        public DataShareCodeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<DataShareCode?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Patient)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(d => d.Code == code, cancellationToken);
        }

        public async Task<DataShareCode?> GetByCodeWithRecordsAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Patient)
                    .ThenInclude(p => p.User)
                .Include(d => d.RecordAccesses)
                    .ThenInclude(r => r.MedicalRecord)
                        .ThenInclude(m => m.Documents)
                .Include(d => d.AccessLogs)
                .FirstOrDefaultAsync(d => d.Code == code, cancellationToken);
        }

        public async Task<IEnumerable<DataShareCode>> GetActiveCodesByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.PatientId == patientId &&
                           d.Status == Domain.Enums.DataShareStatus.Active)
                .Include(d => d.RecordAccesses)
                .Include(d => d.AccessLogs)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<DataShareCode>> GetExpiredCodesAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(d => d.Status == Domain.Enums.DataShareStatus.Active &&
                           d.ExpiresAt.HasValue &&
                           d.ExpiresAt.Value < now)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsCodeUniqueAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return !await _dbSet.AnyAsync(d => d.Code == code, cancellationToken);
        }
    }
}

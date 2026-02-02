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
    public class DataShareRecordAccessRepository : Repository<DataShareRecordAccess>, IDataShareRecordAccessRepository
    {
        public DataShareRecordAccessRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<DataShareRecordAccess>> GetByDataShareCodeIdAsync(
            Guid dataShareCodeId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.DataShareCodeId == dataShareCodeId)
                .Include(r => r.MedicalRecord)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<DataShareRecordAccess>> GetByMedicalRecordIdAsync(
            Guid medicalRecordId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.MedicalRecordId == medicalRecordId)
                .Include(r => r.DataShareCode)
                    .ThenInclude(d => d.Patient)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}

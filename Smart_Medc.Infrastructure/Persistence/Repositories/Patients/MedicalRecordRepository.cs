using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Interfaces.Repositories.Patients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Patients
{
    public class MedicalRecordRepository : Repository<MedicalRecord>, IMedicalRecordRepository
    {
        public MedicalRecordRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(
            Guid patientId,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(mr => mr.Documents)
                .Where(mr => mr.PatientId == patientId);

            if (!includeDeleted)
            {
                query = query.Where(mr => !mr.IsDeleted);
            }

            return await query
                .OrderByDescending(mr => mr.RecordDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<MedicalRecord?> GetByIdWithDocumentsAsync(
            Guid recordId,
            CancellationToken cancellationToken = default)
        {
            return await _context.MedicalRecords
                .Include(r => r.Documents.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);
        }

        public async Task<IEnumerable<MedicalRecord>> GetByPatientIdAndTypeAsync(
            Guid patientId,
            int recordType,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(mr => mr.Documents.Where(d => !d.IsDeleted))
                .Where(mr => mr.PatientId == patientId &&
                            !mr.IsDeleted &&
                            (int)mr.RecordType == recordType)
                .OrderByDescending(mr => mr.RecordDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<MedicalRecord>> GetByDateRangeAsync(
            Guid patientId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(mr => mr.Documents.Where(d => !d.IsDeleted))
                .Where(mr => mr.PatientId == patientId &&
                            !mr.IsDeleted &&
                            mr.RecordDate >= startDate &&
                            mr.RecordDate <= endDate)
                .OrderByDescending(mr => mr.RecordDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<MedicalRecord>> GetSharedRecordsAsync(
            Guid patientId,
            Guid dataShareCodeId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(mr => mr.Documents.Where(d => !d.IsDeleted))
                .Include(mr => mr.SharedAccesses)
                .Where(mr => mr.PatientId == patientId &&
                            !mr.IsDeleted &&
                            mr.SharedAccesses.Any(sa => sa.DataShareCodeId == dataShareCodeId))
                .OrderByDescending(mr => mr.RecordDate)
                .ToListAsync(cancellationToken);
        }
    }
}
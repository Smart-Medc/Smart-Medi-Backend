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
    public class MedicalRecordDocumentRepository : Repository<MedicalRecordDocument>, IMedicalRecordDocumentRepository
    {
        public MedicalRecordDocumentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MedicalRecordDocument>> GetByMedicalRecordIdAsync(
            Guid medicalRecordId,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(d => d.MedicalRecordId == medicalRecordId);

            if (!includeDeleted)
            {
                query = query.Where(d => !d.IsDeleted);
            }

            return await query
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetTotalSizeByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.MedicalRecord)
                .Where(d => d.MedicalRecord.PatientId == patientId &&
                           !d.IsDeleted &&
                           !d.MedicalRecord.IsDeleted)
                .SumAsync(d => d.FileSizeBytes, cancellationToken);
        }
    }
}
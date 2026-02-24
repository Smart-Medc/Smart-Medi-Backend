using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Interfaces.Repositories.Patients;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Patients
{
    public class PatientRepository : Repository<Patient>, IPatientRepository
    {
        public PatientRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Patient?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        }

        public async Task<Patient?> GetByIdWithUserAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        }

        public async Task<Patient?> GetByIdWithMedicalRecordsAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.MedicalRecords.Where(mr => !mr.IsDeleted))
                    .ThenInclude(mr => mr.Documents.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        }

        public async Task<bool> UpdateStorageUsedAsync(
            Guid patientId,
            long bytes,
            CancellationToken cancellationToken = default)
        {
            var patient = await _dbSet.FindAsync(new object[] { patientId }, cancellationToken);
            if (patient == null)
                return false;

            patient.StorageUsedBytes += bytes;
            patient.UpdatedAt = DateTime.UtcNow;

            _dbSet.Update(patient);
            return true;
        }

        public async Task<long> GetStorageUsedAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            var patient = await _dbSet.FindAsync(new object[] { patientId }, cancellationToken);
            return patient?.StorageUsedBytes ?? 0;
        }
    }
}
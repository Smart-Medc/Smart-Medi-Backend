using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Interfaces.Repositories.Patients;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Patients
{
    public class MedicationRepository : Repository<Medication>, IMedicationRepository
    {
        public MedicationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Medication>> GetByPatientIdAsync(
            Guid patientId,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(m => m.Reminders)
                .Include(m => m.AdherenceLogs)
                .Where(m => m.PatientId == patientId);

            if (!includeDeleted)
            {
                query = query.Where(m => !m.IsDeleted);
            }

            return await query
                .OrderByDescending(m => m.StartDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Medication>> GetActiveMedicationsByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Reminders.Where(r => r.IsActive))
                .Include(m => m.AdherenceLogs)
                .Where(m => m.PatientId == patientId &&
                           !m.IsDeleted &&
                           m.Status == Domain.Enums.MedicationStatus.Active)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Medication?> GetByIdWithRemindersAsync(
            Guid medicationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Reminders)
                .Include(m => m.AdherenceLogs)
                .FirstOrDefaultAsync(m => m.Id == medicationId && !m.IsDeleted,
                                    cancellationToken);
        }

        public async Task<IEnumerable<Medication>> GetMedicationsWithInteractionsAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Reminders)
                .Where(m => m.PatientId == patientId &&
                           !m.IsDeleted &&
                           m.Status == Domain.Enums.MedicationStatus.Active &&
                           m.HasInteraction)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
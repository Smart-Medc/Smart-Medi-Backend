using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Interfaces.Repositories.Patients;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Patients
{
    public class MedicationReminderRepository : Repository<MedicationReminder>, IMedicationReminderRepository
    {
        public MedicationReminderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MedicationReminder>> GetByMedicationIdAsync(
            Guid medicationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.MedicationId == medicationId)
                .OrderBy(r => r.ReminderTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<MedicationReminder>> GetActiveRemindersByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(r => r.Medication)
                .Where(r => r.Medication.PatientId == patientId &&
                           r.IsActive &&
                           !r.Medication.IsDeleted &&
                           r.Medication.Status == Domain.Enums.MedicationStatus.Active)
                .OrderBy(r => r.ReminderTime)
                .ToListAsync(cancellationToken);
        }
    }
}
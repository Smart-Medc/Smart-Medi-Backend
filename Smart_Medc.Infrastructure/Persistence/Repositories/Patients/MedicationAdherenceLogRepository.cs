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
    public class MedicationAdherenceLogRepository : Repository<MedicationAdherenceLog>, IMedicationAdherenceLogRepository
    {
        public MedicationAdherenceLogRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<MedicationAdherenceLog>> GetByMedicationIdAsync(
            Guid medicationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.MedicationId == medicationId)
                .OrderByDescending(l => l.ScheduledTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<MedicationAdherenceLog>> GetByDateRangeAsync(
            Guid medicationId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.MedicationId == medicationId &&
                           l.ScheduledTime >= startDate &&
                           l.ScheduledTime <= endDate)
                .OrderByDescending(l => l.ScheduledTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<double> GetAdherenceRateAsync(
            Guid medicationId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            var logs = await _dbSet
                .Where(l => l.MedicationId == medicationId &&
                           l.ScheduledTime >= startDate &&
                           l.ScheduledTime <= endDate)
                .ToListAsync(cancellationToken);

            if (!logs.Any())
                return 0;

            var takenCount = logs.Count(l => l.Status == Domain.Enums.AdherenceStatus.Taken);
            return (double)takenCount / logs.Count * 100;
        }
    }
}
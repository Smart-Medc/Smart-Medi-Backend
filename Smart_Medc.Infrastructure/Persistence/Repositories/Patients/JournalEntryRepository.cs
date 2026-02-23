using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Interfaces.Repositories.Patients;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Patients
{
    public class JournalEntryRepository : Repository<JournalEntry>, IJournalEntryRepository
    {
        public JournalEntryRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<JournalEntry>> GetByPatientIdAsync(
            Guid patientId,
            bool includeDeleted = false,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(je => je.Tags)
                .Where(je => je.PatientId == patientId);

            if (!includeDeleted)
            {
                query = query.Where(je => !je.IsDeleted);
            }

            return await query
                .OrderByDescending(je => je.EntryDate)
                .ThenByDescending(je => je.EntryTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<JournalEntry?> GetByIdWithTagsAsync(
            Guid journalEntryId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(je => je.Tags)
                .FirstOrDefaultAsync(je => je.Id == journalEntryId && !je.IsDeleted,
                                    cancellationToken);
        }

        public async Task<IEnumerable<JournalEntry>> GetByDateRangeAsync(
            Guid patientId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(je => je.Tags)
                .Where(je => je.PatientId == patientId &&
                            !je.IsDeleted &&
                            je.EntryDate >= startDate &&
                            je.EntryDate <= endDate)
                .OrderByDescending(je => je.EntryDate)
                .ThenByDescending(je => je.EntryTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<JournalEntry>> GetByTagAsync(
            Guid patientId,
            string tag,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(je => je.Tags)
                .Where(je => je.PatientId == patientId &&
                            !je.IsDeleted &&
                            je.Tags.Any(t => t.Tag == tag))
                .OrderByDescending(je => je.EntryDate)
                .ThenByDescending(je => je.EntryTime)
                .ToListAsync(cancellationToken);
        }
    }
}
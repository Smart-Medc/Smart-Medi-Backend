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
    public class JournalEntryTagRepository : Repository<JournalEntryTag>, IJournalEntryTagRepository
    {
        public JournalEntryTagRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<JournalEntryTag>> GetByJournalEntryIdAsync(
            Guid journalEntryId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.JournalEntryId == journalEntryId)
                .OrderBy(t => t.Tag)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<string>> GetDistinctTagsByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.JournalEntry)
                .Where(t => t.JournalEntry.PatientId == patientId &&
                           !t.JournalEntry.IsDeleted)
                .Select(t => t.Tag)
                .Distinct()
                .OrderBy(tag => tag)
                .ToListAsync(cancellationToken);
        }
    }
}
using Smart_Medc.Domain.Entities.PatientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Patients
{
    public interface IJournalEntryRepository : IRepository<JournalEntry>
    {
        Task<IEnumerable<JournalEntry>> GetByPatientIdAsync(Guid patientId, bool includeDeleted = false, CancellationToken cancellationToken = default);
        Task<JournalEntry?> GetByIdWithTagsAsync(Guid journalEntryId, CancellationToken cancellationToken = default);
        Task<IEnumerable<JournalEntry>> GetByDateRangeAsync(Guid patientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<JournalEntry>> GetByTagAsync(Guid patientId, string tag, CancellationToken cancellationToken = default);
    }
}

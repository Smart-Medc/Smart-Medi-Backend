using Smart_Medc.Domain.Entities.PatientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Patients
{
    public interface IJournalEntryTagRepository : IRepository<JournalEntryTag>
    {
        Task<IEnumerable<JournalEntryTag>> GetByJournalEntryIdAsync(Guid journalEntryId, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetDistinctTagsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    }
}

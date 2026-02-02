using Smart_Medc.Domain.Entities.PatientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Patients
{
    public interface IMedicationReminderRepository : IRepository<MedicationReminder>
    {
        Task<IEnumerable<MedicationReminder>> GetByMedicationIdAsync(Guid medicationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<MedicationReminder>> GetActiveRemindersByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    }
}

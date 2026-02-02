using Smart_Medc.Domain.Entities.AppointmentModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Appointments
{
    public interface IAppointmentReminderRepository : IRepository<AppointmentReminder>
    {
        Task<IEnumerable<AppointmentReminder>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AppointmentReminder>> GetPendingRemindersAsync(DateTime upToDate, CancellationToken cancellationToken = default);
    }
}

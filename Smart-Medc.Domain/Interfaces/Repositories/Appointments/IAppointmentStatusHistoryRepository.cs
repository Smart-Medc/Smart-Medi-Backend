using Smart_Medc.Domain.Entities.AppointmentModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Appointments
{
    public interface IAppointmentStatusHistoryRepository : IRepository<AppointmentStatusHistory>
    {
        Task<IEnumerable<AppointmentStatusHistory>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    }
}

using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AppointmentModels;
using Smart_Medc.Domain.Interfaces.Repositories.Appointments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Appointments
{
    public class AppointmentReminderRepository : Repository<AppointmentReminder>, IAppointmentReminderRepository
    {
        public AppointmentReminderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<AppointmentReminder>> GetByAppointmentIdAsync(
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.AppointmentId == appointmentId)
                .OrderBy(r => r.ScheduledFor)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AppointmentReminder>> GetPendingRemindersAsync(
            DateTime upToDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(r => r.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.User)
                .Include(r => r.Appointment)
                    .ThenInclude(a => a.Organization)
                .Where(r => !r.IsSent && r.ScheduledFor <= upToDate)
                .OrderBy(r => r.ScheduledFor)
                .ToListAsync(cancellationToken);
        }
    }
}

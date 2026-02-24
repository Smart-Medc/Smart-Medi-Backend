using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AppointmentModels;
using Smart_Medc.Domain.Interfaces.Repositories.Appointments;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Appointments
{
    public class AppointmentStatusHistoryRepository : Repository<AppointmentStatusHistory>, IAppointmentStatusHistoryRepository
    {
        public AppointmentStatusHistoryRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<AppointmentStatusHistory>> GetByAppointmentIdAsync(
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(h => h.AppointmentId == appointmentId)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}

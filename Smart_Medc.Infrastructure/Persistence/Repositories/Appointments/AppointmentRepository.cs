using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AppointmentModels;
using Smart_Medc.Domain.Interfaces.Repositories.Appointments;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Appointments
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Appointment?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Organization)
                .Include(a => a.Doctor)
                .Include(a => a.DataShareCode)
                .Include(a => a.Reminders.OrderBy(r => r.ScheduledFor))
                .Include(a => a.StatusHistory.OrderByDescending(sh => sh.CreatedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Appointment?> GetByAppointmentNumberAsync(
            string appointmentNumber,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Organization)
                .Include(a => a.Doctor)
                .Include(a => a.DataShareCode)
                .FirstOrDefaultAsync(a => a.AppointmentNumber == appointmentNumber, cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.PatientId == patientId)
                .Include(a => a.Organization)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.OrganizationId == organizationId)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);

            return await _dbSet
                .Where(a => a.PatientId == patientId &&
                           (a.AppointmentDate > today ||
                           (a.AppointmentDate == today && a.StartTime > now)))
                .Include(a => a.Organization)
                .Include(a => a.Doctor)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(
            Guid organizationId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.OrganizationId == organizationId &&
                           a.AppointmentDate >= startDate &&
                           a.AppointmentDate <= endDate)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Appointment>> GetByStatusAsync(
            int status,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => (int)a.Status == status)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Organization)
                .Include(a => a.Doctor)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasConflictingAppointmentAsync(
    Guid organizationId,
    DateTime appointmentDate,
    TimeSpan startTime,
    TimeSpan endTime,
    Guid? excludeAppointmentId = null,
    CancellationToken cancellationToken = default)
        {
            var dateOnly = appointmentDate.Date;

            var query = _dbSet
                .Where(a => a.OrganizationId == organizationId &&
                            // CHANGED: use .Date to strip time component from stored
                            // AppointmentDate values so legacy/seeded records with
                            // non-midnight DateTime values are compared correctly.
                            a.AppointmentDate.Date == dateOnly &&
                            // CHANGED: added Rejected — rejected appointments must not
                            // hold the slot open since the org has explicitly declined them.
                            a.Status != Domain.Enums.AppointmentStatus.Cancelled &&
                            a.Status != Domain.Enums.AppointmentStatus.Rejected &&
                            a.Status != Domain.Enums.AppointmentStatus.NoShow &&
                            // Overlap condition: new slot overlaps if it starts before
                            // existing ends AND ends after existing starts.
                            a.StartTime < TimeOnly.FromTimeSpan(endTime) &&
                            a.EndTime > TimeOnly.FromTimeSpan(startTime));

            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAppointmentId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
    }
}

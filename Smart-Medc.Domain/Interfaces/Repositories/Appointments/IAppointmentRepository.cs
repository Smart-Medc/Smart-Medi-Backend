using Smart_Medc.Domain.Entities.AppointmentModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Appointments
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<Appointment?> GetByAppointmentNumberAsync(string appointmentNumber, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetByDateRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<Appointment>> GetByStatusAsync(int status, CancellationToken cancellationToken = default);
        Task<bool> HasConflictingAppointmentAsync(Guid organizationId, DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default);
    }
}

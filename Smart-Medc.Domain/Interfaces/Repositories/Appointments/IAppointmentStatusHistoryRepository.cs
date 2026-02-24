using Smart_Medc.Domain.Entities.AppointmentModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Appointments
{
    public interface IAppointmentStatusHistoryRepository : IRepository<AppointmentStatusHistory>
    {
        Task<IEnumerable<AppointmentStatusHistory>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    }
}

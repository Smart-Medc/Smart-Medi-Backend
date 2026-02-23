using Smart_Medc.Domain.Entities.AppointmentModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Appointments
{
    public interface IAppointmentReminderRepository : IRepository<AppointmentReminder>
    {
        Task<IEnumerable<AppointmentReminder>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AppointmentReminder>> GetPendingRemindersAsync(DateTime upToDate, CancellationToken cancellationToken = default);
    }
}

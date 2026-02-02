using Smart_Medc.Domain.Entities.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Notifications
{
    public interface IPatientNotificationRepository : IRepository<PatientNotification>
    {
        Task<IEnumerable<PatientNotification>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PatientNotification>> GetUnreadByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(Guid patientId, CancellationToken cancellationToken = default);
    }
}

using Microsoft.AspNetCore.SignalR;
using Smart_Medc.Application.Interfaces.Notifications;

namespace Smart_Medc.API.Hubs
{
    /// <summary>
    /// Concrete implementation of INotificationHubPusher.
    /// Lives in API layer so it can reference IHubContext freely.
    /// Registered in DI as the binding for INotificationHubPusher.
    /// </summary>
    public class NotificationHubPusher : INotificationHubPusher
    {
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public NotificationHubPusher(
            IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PushToUserAsync(
            string userId,
            string methodName,
            object payload,
            CancellationToken ct = default)
        {
            switch (methodName)
            {
                case "ReceiveNotification":
                    await _hubContext.Clients
                        .Group(userId)
                        .ReceiveNotification(payload);
                    break;

                case "UnreadCountUpdated":
                    await _hubContext.Clients
                        .Group(userId)
                        .UnreadCountUpdated(Convert.ToInt32(payload));
                    break;

                default:
                    // Forward generic method names via non-generic IHubContext
                    await _hubContext.Clients
                        .Group(userId)
                        .ReceiveNotification(payload);
                    break;
            }
        }
    }
}

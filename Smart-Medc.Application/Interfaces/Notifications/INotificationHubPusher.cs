    
namespace Smart_Medc.Application.Interfaces.Notifications
{
    /// <summary>
    /// Thin abstraction over SignalR IHubContext so the Application layer
    /// never references Microsoft.AspNetCore.SignalR directly.
    /// The API layer provides the concrete implementation.
    /// </summary>
    public interface INotificationHubPusher
    {
        /// <summary>
        /// Pushes a message to all SignalR connections in the group
        /// named after the userId string.
        /// </summary>
        Task PushToUserAsync(
            string userId,
            string methodName,
            object payload,
            CancellationToken ct = default);
    }
}

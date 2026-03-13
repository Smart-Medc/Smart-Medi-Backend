namespace Smart_Medc.API.Hubs
{
    /// <summary>
    /// Strongly-typed client interface for NotificationHub.
    /// Every method here is a frontend event the client listens for.
    /// </summary>
    public interface INotificationClient
    {
        /// <summary>
        /// Fired when a new notification is created for this user.
        /// Frontend payload: PatientNotificationDto or OrganizationNotificationDto
        /// </summary>
        Task ReceiveNotification(object notification);

        /// <summary>
        /// Fired after mark-read operations to sync badge counter.
        /// Frontend payload: int (new total unread count)
        /// </summary>
        Task UnreadCountUpdated(int unreadCount);
    }
}

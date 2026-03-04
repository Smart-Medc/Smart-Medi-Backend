using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Smart_Medc.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(
            INotificationService notificationService,
            IUnitOfWork uow,
            ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _uow = uow;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                Context.Abort();
                return;
            }

            // Each user joins a group named after their UserId.
            // This allows INotificationHubPusher to target them by UserId.
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());

            // Push initial unread count on connect
            var userType = GetUserType();
            if (userType == UserType.Patient)
            {
                var patient = await _uow.Patients
                    .GetByUserIdAsync(userId.Value);
                if (patient != null)
                {
                    var count = await _uow.PatientNotifications
                        .GetUnreadCountAsync(patient.Id);
                    await Clients.Caller.UnreadCountUpdated(count);
                }
            }
            else if (userType == UserType.Organization)
            {
                var org = await _uow.Organizations
                    .GetByUserIdAsync(userId.Value);
                if (org != null)
                {
                    var count = await _uow.OrganizationNotifications
                        .GetUnreadCountAsync(org.Id);
                    await Clients.Caller.UnreadCountUpdated(count);
                }
            }

            _logger.LogInformation(
                "User {UserId} connected to NotificationHub [{ConnectionId}]",
                userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(
                    Context.ConnectionId, userId.Value.ToString());

                _logger.LogInformation(
                    "User {UserId} disconnected from NotificationHub", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client calls this to mark a single notification as read.
        /// The hub determines whether caller is a patient or org
        /// and routes to the correct service method.
        /// </summary>
        public async Task MarkAsRead(Guid notificationId)
        {
            var userId = GetUserId();
            if (userId == null) return;

            var userType = GetUserType();
            if (userType == UserType.Patient)
            {
                var patient = await _uow.Patients.GetByUserIdAsync(userId.Value);
                if (patient != null)
                    await _notificationService.MarkPatientNotificationAsReadAsync(
                        patient.Id, notificationId);
            }
            else if (userType == UserType.Organization)
            {
                var org = await _uow.Organizations.GetByUserIdAsync(userId.Value);
                if (org != null)
                    await _notificationService.MarkOrganizationNotificationAsReadAsync(
                        org.Id, notificationId);
            }
        }

        /// <summary>
        /// Client calls this to mark all notifications as read.
        /// </summary>
        public async Task MarkAllAsRead()
        {
            var userId = GetUserId();
            if (userId == null) return;

            var userType = GetUserType();
            if (userType == UserType.Patient)
            {
                var patient = await _uow.Patients.GetByUserIdAsync(userId.Value);
                if (patient != null)
                    await _notificationService.MarkAllPatientNotificationsAsReadAsync(patient.Id);
            }
            else if (userType == UserType.Organization)
            {
                var org = await _uow.Organizations.GetByUserIdAsync(userId.Value);
                if (org != null)
                    await _notificationService.MarkAllOrganizationNotificationsAsReadAsync(org.Id);
            }
        }

        private Guid? GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        private UserType GetUserType()
        {
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            return role switch
            {
                "Patient" => UserType.Patient,
                "Organization" => UserType.Organization,
                "Admin" => UserType.Admin,
                _ => UserType.Patient
            };
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.Notifications;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/organizations/{organizationId}/notifications")]
    [Authorize(Roles = "Organization")]
    public class OrganizationNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _uow;

        public OrganizationNotificationsController(
            INotificationService notificationService,
            IUnitOfWork uow)
        {
            _notificationService = notificationService;
            _uow = uow;
        }

        // ─────────────────────────────────────────────────
        // GET  api/organizations/{organizationId}/notifications
        // ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            Guid organizationId,
            [FromQuery] bool? isRead = null,
            [FromQuery] OrganizationNotificationType? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(organizationId, ct))
                return Forbid();

            var result = await _notificationService.GetOrganizationNotificationsAsync(
                organizationId, isRead, type, page, pageSize, ct);

            return Ok(new { success = true, data = result });
        }

        // ─────────────────────────────────────────────────
        // GET  api/organizations/{organizationId}/notifications/unread-count
        // ─────────────────────────────────────────────────
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(
            Guid organizationId,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(organizationId, ct))
                return Forbid();

            var result = await _notificationService
                .GetOrganizationUnreadCountAsync(organizationId, ct);

            return Ok(new { success = true, data = result });
        }

        // ─────────────────────────────────────────────────
        // PUT  api/organizations/{organizationId}/notifications/{notificationId}/read
        // ─────────────────────────────────────────────────
        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(
            Guid organizationId,
            Guid notificationId,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(organizationId, ct))
                return Forbid();

            await _notificationService.MarkOrganizationNotificationAsReadAsync(
                organizationId, notificationId, ct);

            return NoContent();
        }

        // ─────────────────────────────────────────────────
        // PUT  api/organizations/{organizationId}/notifications/read-batch
        // ─────────────────────────────────────────────────
        [HttpPut("read-batch")]
        public async Task<IActionResult> MarkBatchAsRead(
            Guid organizationId,
            [FromBody] MarkReadBatchDto dto,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(organizationId, ct))
                return Forbid();

            await _notificationService.MarkOrganizationNotificationsBatchAsReadAsync(
                organizationId, dto.NotificationIds, ct);

            return NoContent();
        }

        // ─────────────────────────────────────────────────
        // PUT  api/organizations/{organizationId}/notifications/read-all
        // ─────────────────────────────────────────────────
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead(
            Guid organizationId,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(organizationId, ct))
                return Forbid();

            await _notificationService
                .MarkAllOrganizationNotificationsAsReadAsync(organizationId, ct);

            return NoContent();
        }

        // ─────────────────────────────────────────────────
        // GET  api/organizations/{organizationId}/notifications/preferences
        // ─────────────────────────────────────────────────
        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences(
            Guid organizationId,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(organizationId, ct))
                return Forbid();

            var org = await _uow.Organizations
                .GetByIdWithDetailsAsync(organizationId, ct);
            if (org == null) return NotFound();

            var result = await _notificationService
                .GetUserPreferencesAsync(org.UserId, ct);

            return Ok(new { success = true, data = result });
        }

        // ─────────────────────────────────────────────────
        // PUT  api/organizations/{organizationId}/notifications/preferences
        // ─────────────────────────────────────────────────
        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences(
            Guid organizationId,
            [FromBody] UpdateAllNotificationPreferencesDto dto,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(organizationId, ct))
                return Forbid();

            var org = await _uow.Organizations
                .GetByIdWithDetailsAsync(organizationId, ct);
            if (org == null) return NotFound();

            await _notificationService.UpdateUserPreferencesAsync(org.UserId, dto, ct);

            return NoContent();
        }

        // ─────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────
        private async Task<bool> IsCallerOwnerAsync(Guid organizationId, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return false;

            var org = await _uow.Organizations.GetByUserIdAsync(userId, ct);
            return org?.Id == organizationId;
        }
    }
}

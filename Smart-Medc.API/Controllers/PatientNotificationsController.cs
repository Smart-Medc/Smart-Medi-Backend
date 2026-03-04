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
    [Route("api/patients/{patientId}/notifications")]
    [Authorize(Roles = "Patient")]
    public class PatientNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _uow;

        public PatientNotificationsController(
            INotificationService notificationService,
            IUnitOfWork uow)
        {
            _notificationService = notificationService;
            _uow = uow;
        }

        // ─────────────────────────────────────────────────
        // GET  api/patients/{patientId}/notifications
        // Query params: isRead, type, page, pageSize
        // ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            Guid patientId,
            [FromQuery] bool? isRead = null,
            [FromQuery] PatientNotificationType? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(patientId, ct))
                return Forbid();

            var result = await _notificationService.GetPatientNotificationsAsync(
                patientId, isRead, type, page, pageSize, ct);

            return Ok(new { success = true, data = result });
        }

        // ─────────────────────────────────────────────────
        // GET  api/patients/{patientId}/notifications/unread-count
        // ─────────────────────────────────────────────────
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(
            Guid patientId,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(patientId, ct))
                return Forbid();

            var result = await _notificationService
                .GetPatientUnreadCountAsync(patientId, ct);

            return Ok(new { success = true, data = result });
        }

        // ─────────────────────────────────────────────────
        // PUT  api/patients/{patientId}/notifications/{notificationId}/read
        // ─────────────────────────────────────────────────
        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(
            Guid patientId,
            Guid notificationId,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(patientId, ct))
                return Forbid();

            await _notificationService.MarkPatientNotificationAsReadAsync(
                patientId, notificationId, ct);

            return NoContent();
        }

        // ─────────────────────────────────────────────────
        // PUT  api/patients/{patientId}/notifications/read-batch
        // Body: { "notificationIds": ["guid","guid"] }
        // ─────────────────────────────────────────────────
        [HttpPut("read-batch")]
        public async Task<IActionResult> MarkBatchAsRead(
            Guid patientId,
            [FromBody] MarkReadBatchDto dto,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(patientId, ct))
                return Forbid();

            await _notificationService.MarkPatientNotificationsBatchAsReadAsync(
                patientId, dto.NotificationIds, ct);

            return NoContent();
        }

        // ─────────────────────────────────────────────────
        // PUT  api/patients/{patientId}/notifications/read-all
        // ─────────────────────────────────────────────────
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead(
            Guid patientId,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(patientId, ct))
                return Forbid();

            await _notificationService.MarkAllPatientNotificationsAsReadAsync(patientId, ct);

            return NoContent();
        }

        // ─────────────────────────────────────────────────
        // GET  api/patients/{patientId}/notifications/preferences
        // ─────────────────────────────────────────────────
        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences(
            Guid patientId,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(patientId, ct))
                return Forbid();

            var patient = await _uow.Patients.GetByIdWithUserAsync(patientId, ct);
            if (patient == null) return NotFound();

            var result = await _notificationService
                .GetUserPreferencesAsync(patient.UserId, ct);

            return Ok(new { success = true, data = result });
        }

        // ─────────────────────────────────────────────────
        // PUT  api/patients/{patientId}/notifications/preferences
        // ─────────────────────────────────────────────────
        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences(
            Guid patientId,
            [FromBody] UpdateAllNotificationPreferencesDto dto,
            CancellationToken ct = default)
        {
            if (!await IsCallerOwnerAsync(patientId, ct))
                return Forbid();

            var patient = await _uow.Patients.GetByIdWithUserAsync(patientId, ct);
            if (patient == null) return NotFound();

            await _notificationService.UpdateUserPreferencesAsync(patient.UserId, dto, ct);

            return NoContent();
        }

        // ─────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────
        private async Task<bool> IsCallerOwnerAsync(Guid patientId, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return false;

            var patient = await _uow.Patients.GetByUserIdAsync(userId, ct);
            return patient?.Id == patientId;
        }
    }
}

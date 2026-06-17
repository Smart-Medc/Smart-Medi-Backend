using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.AI;
using Smart_Medc.Application.Interfaces;
using System.Security.Claims;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:guid}/ai-chat")]
    [Authorize(Roles = "Patient")]
    public class AIChatController : ControllerBase
    {
        private readonly IAIChatService _aiChatService;
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(IAIChatService aiChatService, ILogger<AIChatController> logger)
        {
            _aiChatService = aiChatService;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════
        // SESSION ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// POST: api/patients/{patientId}/ai-chat/sessions
        /// Create a new AI chat session
        /// </summary>
        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession(
            Guid patientId,
            [FromBody] CreateAIChatSessionDto dto,
            CancellationToken ct = default)
        {
            var authenticatedUserId = GetAuthenticatedUserId();
            if (!ValidatePatientOwnership(patientId, authenticatedUserId))
                return Forbid();

            var result = await _aiChatService.CreateSessionAsync(patientId, dto, ct);
            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return CreatedAtAction(nameof(GetSessionDetails),
                new { patientId, sessionId = result.Data!.Id },
                new { success = true, data = result.Data });
        }

        /// <summary>
        /// GET: api/patients/{patientId}/ai-chat/sessions?pageNumber=1&pageSize=20
        /// Get patient's chat sessions (paginated)
        /// </summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(
            Guid patientId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var authenticatedUserId = GetAuthenticatedUserId();
            if (!ValidatePatientOwnership(patientId, authenticatedUserId))
                return Forbid();

            var result = await _aiChatService.GetPatientSessionsAsync(patientId, pageNumber, pageSize, ct);
            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new { success = true, data = result.Data });
        }

        /// <summary>
        /// GET: api/patients/{patientId}/ai-chat/sessions/{sessionId}
        /// Get a specific session with all messages
        /// </summary>
        [HttpGet("sessions/{sessionId:guid}")]
        public async Task<IActionResult> GetSessionDetails(
            Guid patientId,
            Guid sessionId,
            CancellationToken ct = default)
        {
            var authenticatedUserId = GetAuthenticatedUserId();
            if (!ValidatePatientOwnership(patientId, authenticatedUserId))
                return Forbid();

            var result = await _aiChatService.GetSessionDetailsAsync(patientId, sessionId, ct);
            if (!result.IsSuccess)
                return NotFound(new { success = false, message = result.ErrorMessage });

            return Ok(new { success = true, data = result.Data });
        }

        /// <summary>
        /// DELETE: api/patients/{patientId}/ai-chat/sessions/{sessionId}
        /// Delete a chat session (soft delete)
        /// </summary>
        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<IActionResult> DeleteSession(
            Guid patientId,
            Guid sessionId,
            CancellationToken ct = default)
        {
            var authenticatedUserId = GetAuthenticatedUserId();
            if (!ValidatePatientOwnership(patientId, authenticatedUserId))
                return Forbid();

            var result = await _aiChatService.DeleteSessionAsync(patientId, sessionId, ct);
            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return NoContent();
        }

        // ═══════════════════════════════════════════════════════════════
        // MESSAGING ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// POST: api/patients/{patientId}/ai-chat/sessions/{sessionId}/messages
        /// Send a message and get streaming response via SignalR
        /// This initiates the chat stream. Client listens to NotificationHub for real-time chunks.
        /// </summary>
        [HttpPost("sessions/{sessionId:guid}/messages")]
        public async Task<IActionResult> SendMessage(
            Guid patientId,
            Guid sessionId,
            [FromBody] SendAIChatMessageDto dto,
            CancellationToken ct = default)
        {
            var authenticatedUserId = GetAuthenticatedUserId();
            if (!ValidatePatientOwnership(patientId, authenticatedUserId))
                return Forbid();

            // Validation
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Message content is required" });

            dto.SessionId = sessionId;

            // Initiate the chat (creates user message + starts AI processing)
            var result = await _aiChatService.InitiateChatStreamAsync(patientId, dto, ct);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            // Return the user message + AI message ID so client knows what to listen for
            return Accepted(new
            {
                success = true,
                message = "Chat initiated. Streaming response via SignalR...",
                data = result.Data
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // ATTACHMENT ENDPOINTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// GET: api/patients/{patientId}/ai-chat/available-attachments
        /// Get list of patient's medical records that can be attached
        /// (for the file selection modal in React)
        /// </summary>
        [HttpGet("available-attachments")]
        public async Task<IActionResult> GetAvailableAttachments(
            Guid patientId,
            CancellationToken ct = default)
        {
            var authenticatedUserId = GetAuthenticatedUserId();
            if (!ValidatePatientOwnership(patientId, authenticatedUserId))
                return Forbid();

            var result = await _aiChatService.GetAvailableAttachmentsAsync(patientId, ct);
            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new { success = true, data = result.Data });
        }

        /// <summary>
        /// POST: api/patients/{patientId}/ai-chat/sessions/{sessionId}/attachments
        /// Upload a file to attach to the session (stores in R2)
        /// </summary>
        [HttpPost("sessions/{sessionId:guid}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAttachment(
            Guid patientId,
            Guid sessionId,
            [FromForm] IFormFile file,
            CancellationToken ct = default)
        {
            var authenticatedUserId = GetAuthenticatedUserId();
            if (!ValidatePatientOwnership(patientId, authenticatedUserId))
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            var dto = new UploadAIChatAttachmentDto
            {
                SessionId = sessionId,
                File = file
            };

            var result = await _aiChatService.UploadAttachmentAsync(patientId, sessionId, file, ct);
            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return Created("", new { success = true, data = result.Data });
        }

        /// <summary>
        /// DELETE: api/patients/{patientId}/ai-chat/sessions/{sessionId}/messages/{messageId}/attachments/{attachmentId}
        /// Delete an attachment from a message
        /// </summary>
        [HttpDelete("sessions/{sessionId:guid}/messages/{messageId:guid}/attachments/{attachmentId:guid}")]
        public async Task<IActionResult> DeleteAttachment(
            Guid patientId,
            Guid sessionId,
            Guid messageId,
            Guid attachmentId,
            CancellationToken ct = default)
        {
            var authenticatedUserId = GetAuthenticatedUserId();
            if (!ValidatePatientOwnership(patientId, authenticatedUserId))
                return Forbid();

            var result = await _aiChatService.DeleteAttachmentAsync(patientId, messageId, attachmentId, ct);
            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return NoContent();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════

        private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("User ID not found in token");
            return userId;
        }

        private bool ValidatePatientOwnership(Guid patientId, Guid authenticatedUserId)
        {
            // TODO: Verify that authenticatedUserId owns this patientId
            // This check should be in the service layer ideally
            return true; // Placeholder
        }
    }
}
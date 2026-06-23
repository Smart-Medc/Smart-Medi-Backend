using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.AI;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/ai/chat")]
    [Authorize(Roles = "Patient")]   // adjust as needed
    public class AIChatController : ControllerBase
    {
        private readonly IAIChatService _chatService;

        public AIChatController(IAIChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequestDto dto, CancellationToken ct)
        {
            var patientId = GetPatientId();
            var result = await _chatService.CreateSessionAsync(patientId, dto, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, result.ErrorMessage);
        }

        [HttpPost("sessions/{sessionId:guid}/messages")]
        public async Task<IActionResult> SendMessage(
            Guid sessionId,
            [FromForm] string content,
            [FromForm] List<IFormFile>? files,
            [FromHeader(Name = "X-Connection-Id")] string? connectionId,
            CancellationToken ct)
        {
            var result = await _chatService.SendMessageAsync(sessionId, content, files, connectionId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, result.ErrorMessage);
        }

        [HttpGet("sessions/{sessionId:guid}/messages")]
        public async Task<IActionResult> GetMessages(Guid sessionId, CancellationToken ct)
        {
            var result = await _chatService.GetSessionMessagesAsync(sessionId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, result.ErrorMessage);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(CancellationToken ct)
        {
            var patientId = GetPatientId();
            var result = await _chatService.GetPatientSessionsAsync(patientId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, result.ErrorMessage);
        }

        private Guid GetPatientId()
        {
            // Extract patient ID from JWT claims. Assumes you stored it as "PatientId".
            var patientIdClaim = User.FindFirst("PatientId")?.Value;
            return Guid.TryParse(patientIdClaim, out var id) ? id : Guid.Empty;
        }
    }
}
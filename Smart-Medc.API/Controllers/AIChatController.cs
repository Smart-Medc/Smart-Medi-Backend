using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.AI;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    // [Authorize(Roles = "Patient")]
    [Route("api/ai/chat")]
    public class AIChatController : ControllerBase
    {
        private readonly IAIChatService _chatService;

        public AIChatController(IAIChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession(Guid patientId, [FromBody] CreateSessionRequestDto dto, CancellationToken ct)
        {
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
            // Keep consistent response contract
            var result = await _chatService.SendMessageAsync(sessionId, content, files, connectionId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("sessions/{sessionId:guid}/messages")]
        public async Task<IActionResult> GetMessages(Guid sessionId, CancellationToken ct)
        {
            var result = await _chatService.GetSessionMessagesAsync(sessionId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, result.ErrorMessage);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(Guid patientId, CancellationToken ct)
        {
            var result = await _chatService.GetPatientSessionsAsync(patientId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, result.ErrorMessage);
        }

        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken ct)
        {
            var result = await _chatService.DeleteSessionAsync(sessionId, ct);
            return result.IsSuccess
                ? Ok(new { success = true })
                : StatusCode(result.StatusCode, result.ErrorMessage);
        }
        [HttpPut("sessions/{sessionId:guid}/context-options")]
        public async Task<IActionResult> UpdateSessionContextOptions(
            Guid sessionId,
            [FromBody] UpdateSessionContextOptionsDto dto,
            CancellationToken ct)
        {
            var result = await _chatService.UpdateSessionContextOptionsAsync(sessionId, dto, ct);
            return result.IsSuccess
                ? Ok(result.Data)
                : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }
    }
}
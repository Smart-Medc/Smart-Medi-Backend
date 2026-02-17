using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.DataSharing;
using Smart_Medc.Application.Interfaces;
using System.Security.Claims;

namespace Smart_Medc.API.Controllers
{
    /// <summary>
    /// Patient Management of Data Sharing Codes
    /// </summary>
    [ApiController]
    [Route("api/data-sharing")]
    [Authorize(Roles = "Patient")]
    public class DataSharingController : ControllerBase
    {
        private readonly IDataSharingService _service;

        public DataSharingController(IDataSharingService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST: api/data-sharing/generate
        /// Generate a new data share code (Patient only)
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<DataShareCodeDto>> Generate(
            [FromBody] GenerateShareCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            // Get patientId from authenticated user token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                var result = await _service.GenerateShareCodeAsync(patientId, dto, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while generating share code" });
            }
        }

        /// <summary>
        /// GET: api/data-sharing/codes?pageNumber=1&pageSize=20
        /// Get all active share codes for authenticated patient with pagination
        /// </summary>
        [HttpGet("codes")]
        public async Task<ActionResult<PagedResult<DataShareCodeDto>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Get patientId from authenticated user token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                var result = await _service.GetActiveCodesAsync(patientId, pageNumber, pageSize, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while retrieving codes" });
            }
        }

        /// <summary>
        /// PUT: api/data-sharing/codes/{codeId}/revoke
        /// Revoke a share code (Patient only)
        /// </summary>
        [HttpPut("codes/{codeId}/revoke")]
        public async Task<IActionResult> Revoke(
            Guid codeId,
            CancellationToken cancellationToken = default)
        {
            // Get patientId from authenticated user token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                await _service.RevokeCodeAsync(patientId, codeId, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while revoking code" });
            }
        }
    }
}

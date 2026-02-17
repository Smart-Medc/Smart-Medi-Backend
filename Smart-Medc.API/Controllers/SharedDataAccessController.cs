using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.DataSharing;
using Smart_Medc.Application.Interfaces;
using System.Security.Claims;

namespace Smart_Medc.API.Controllers
{
    /// <summary>
    /// External/Organization Viewer Access to Shared Data
    /// </summary>
    [ApiController]
    [Route("api/shared-data")]
    public class SharedDataAccessController : ControllerBase
    {
        private readonly IDataSharingService _service;

        public SharedDataAccessController(IDataSharingService service)
        {
            _service = service;
        }

        /// <summary>
        /// POST: api/shared-data/validate
        /// Validate a share code and log access
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<ValidateCodeResponseDto>> Validate(
            [FromBody] ValidateCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            // Validate request body
            if (dto == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            // Validate code parameter
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new { message = "Code is required" });
            }

            // Capture IP address and User-Agent for audit logging
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            // Get organization ID if authenticated (optional)
            Guid? organizationId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                // If authenticated and is an Organization user, pass the ID
                if (User.IsInRole("Organization"))
                {
                    organizationId = userId;
                }
            }

            try
            {
                // Pass IP, UserAgent, and OrganizationId to service
                var result = await _service.ValidateCodeAsync(
                    dto.Code,
                    ipAddress,
                    userAgent,
                    organizationId,
                    cancellationToken
                );

                // Return result regardless of IsValid status
                // Business rule violations (expired, revoked) return 200 OK with IsValid=false
                // This allows client to handle different validation failures appropriately
                if (!result.IsValid)
                {
                    return Ok(result); // Changed from BadRequest to Ok for business rule violations
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while validating the code" });
            }
        }

        /// <summary>
        /// GET: api/shared-data/records/{code}?pageNumber=1&pageSize=20
        /// Get shared medical records using a valid code with pagination
        /// </summary>
        [HttpGet("records/{code}")]
        public async Task<ActionResult<PagedResult<SharedMedicalRecordDto>>> GetRecords(
            string code,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Validate code parameter
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { message = "Code is required" });
            }

            // Capture IP address and User-Agent for audit logging
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            // Get organization ID if authenticated (optional)
            Guid? organizationId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                // If authenticated and is an Organization user, pass the ID
                if (User.IsInRole("Organization"))
                {
                    organizationId = userId;
                }
            }

            try
            {
                var result = await _service.GetSharedRecordsAsync(
                    code,
                    ipAddress,
                    userAgent,
                    organizationId,
                    pageNumber,
                    pageSize,
                    cancellationToken
                );

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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
                return StatusCode(500, new { message = "An error occurred while retrieving records" });
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Organization;
using Smart_Medc.Application.Interfaces;
using System.Security.Claims;

namespace Smart_Medc.API.Controllers
{
    /// <summary>
    /// Organization search and availability (Public endpoints for patients)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationsController : ControllerBase
    {
        private readonly IOrganizationService _service;

        public OrganizationsController(IOrganizationService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET: api/organizations/search?searchTerm=...&city=...&type=...&pageNumber=1&pageSize=10
        /// Search organizations with filters (Public endpoint)
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<OrganizationSearchResultDto>>> Search(
            [FromQuery] OrganizationSearchDto query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _service.SearchAsync(query, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while searching organizations" });
            }
        }

        /// <summary>
        /// GET: api/organizations/{organizationId}
        /// Get organization details (Public endpoint)
        /// </summary>
        [HttpGet("{organizationId}")]
        public async Task<ActionResult<OrganizationDetailDto>> Get(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            // Validate organizationId
            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid organization ID" });
            }

            try
            {
                var result = await _service.GetOrganizationDetailsAsync(organizationId, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while retrieving organization details" });
            }
        }

        /// <summary>
        /// GET: api/organizations/{organizationId}/availability?month=2026-03-01
        /// Get month availability calendar (Public endpoint)
        /// </summary>
        [HttpGet("{organizationId}/availability")]
        public async Task<ActionResult<List<AvailabilityDto>>> GetAvailability(
            Guid organizationId,
            [FromQuery] DateTime month,
            CancellationToken cancellationToken = default)
        {
            // Validate organizationId
            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid organization ID" });
            }

            try
            {
                var result = await _service.GetMonthAvailabilityAsync(organizationId, month, cancellationToken);
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
                return StatusCode(500, new { message = "An error occurred while retrieving availability" });
            }
        }

        /// <summary>
        /// GET: api/organizations/{organizationId}/time-slots?date=2026-03-22
        /// Get daily time slots for booking (Public endpoint)
        /// </summary>
        [HttpGet("{organizationId}/time-slots")]
        public async Task<ActionResult<List<TimeSlotDto>>> GetTimeSlots(
            Guid organizationId,
            [FromQuery] DateTime date,
            CancellationToken cancellationToken = default)
        {
            // Validate organizationId
            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid organization ID" });
            }

            try
            {
                var result = await _service.GetDailyTimeSlotsAsync(organizationId, date, cancellationToken);
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
                return StatusCode(500, new { message = "An error occurred while retrieving time slots" });
            }
        }

        /// <summary>
        /// GET: api/organizations/{organizationId}/operating-hours
        /// Get the editable weekly operating hours schedule (Organization only)
        /// </summary>
        [HttpGet("{organizationId}/operating-hours")]
        [Authorize(Roles = "Organization")]
        public async Task<ActionResult<List<GetOperatingHoursDto>>> GetOperatingHours(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
                return BadRequest(new { message = "Invalid organization ID" });

            try
            {
                var result = await _service.GetOperatingHoursAsync(
                    organizationId, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving operating hours" });
            }
        }

        /// <summary>
        /// PUT: api/organizations/{organizationId}/operating-hours
        /// Save the weekly operating hours schedule (Organization only)
        /// </summary>
        [HttpPut("{organizationId}/operating-hours")]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> UpdateOperatingHours(
            Guid organizationId,
            [FromBody] UpdateOperatingHoursRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
                return BadRequest(new { message = "Invalid organization ID" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) ||
                !Guid.TryParse(userIdClaim, out var requestingUserId))
                return Unauthorized(new { message = "User ID not found in token" });

            try
            {
                await _service.UpdateOperatingHoursAsync(
                    organizationId, requestingUserId, dto, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while updating operating hours" });
            }
        }

        /// <summary>
        /// GET: api/organizations/{organizationId}/exceptions?date=2026-12-25
        /// Get the availability exception for a specific date (Organization only)
        /// </summary>
        [HttpGet("{organizationId}/exceptions")]
        [Authorize(Roles = "Organization")]
        public async Task<ActionResult<AvailabilityExceptionDto>> GetException(
            Guid organizationId,
            [FromQuery] DateTime date,
            CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
                return BadRequest(new { message = "Invalid organization ID" });

            try
            {
                var result = await _service.GetExceptionByDateAsync(
                    organizationId, date, cancellationToken);

                // Return 204 when no exception exists for this date —
                // the frontend treats this as "using the weekly template"
                if (result == null)
                    return NoContent();

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the exception" });
            }
        }

        /// <summary>
        /// PUT: api/organizations/{organizationId}/exceptions?date=2026-12-25
        /// Create or update the availability exception for a specific date (Organization only)
        /// </summary>
        [HttpPut("{organizationId}/exceptions")]
        [Authorize(Roles = "Organization")]
        public async Task<ActionResult<AvailabilityExceptionDto>> UpsertException(
            Guid organizationId,
            [FromQuery] DateTime date,
            [FromBody] UpsertAvailabilityExceptionDto dto,
            CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
                return BadRequest(new { message = "Invalid organization ID" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) ||
                !Guid.TryParse(userIdClaim, out var requestingUserId))
                return Unauthorized(new { message = "User ID not found in token" });

            try
            {
                var result = await _service.UpsertExceptionAsync(
                    organizationId, requestingUserId, date, dto, cancellationToken);
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while saving the exception" });
            }
        }

        /// <summary>
        /// DELETE: api/organizations/{organizationId}/exceptions?date=2026-12-25
        /// Delete the exception for a specific date, restoring the weekly template (Organization only)
        /// </summary>
        [HttpDelete("{organizationId}/exceptions")]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> DeleteException(
            Guid organizationId,
            [FromQuery] DateTime date,
            CancellationToken cancellationToken = default)
        {
            if (organizationId == Guid.Empty)
                return BadRequest(new { message = "Invalid organization ID" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) ||
                !Guid.TryParse(userIdClaim, out var requestingUserId))
                return Unauthorized(new { message = "User ID not found in token" });

            try
            {
                await _service.DeleteExceptionAsync(
                    organizationId, requestingUserId, date, cancellationToken);
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the exception" });
            }
        }
    }
}

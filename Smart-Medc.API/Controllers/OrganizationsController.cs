using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Organization;
using Smart_Medc.Application.Interfaces;

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
    }
}

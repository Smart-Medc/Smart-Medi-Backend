using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Appointment;
using Smart_Medc.Application.Interfaces;
using System.Security.Claims;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        // =========================
        // Patient Endpoints
        // =========================

        /// <summary>
        /// POST: api/appointments
        /// Book a new appointment (Patient only)
        /// </summary>
        [HttpPost]
        //[Authorize(Roles = "Patient")]
        public async Task<ActionResult<AppointmentDto>> BookAppointment(
            [FromBody] BookAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            // Extract authenticated patient ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var authenticatedPatientId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                var result = await _appointmentService.BookAppointmentAsync(
                    authenticatedPatientId, dto, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while booking appointment" });
            }
        }

        /// <summary>
        /// GET: api/appointments/patient/{patientId}?status=confirmed&pageNumber=1&pageSize=20
        /// Get patient's appointments with optional status filter and pagination
        /// </summary>
        [HttpGet("patient/{patientId}")]
        //[Authorize(Roles = "Patient")]
        public async Task<ActionResult<PagedResult<AppointmentDto>>> GetPatientAppointments(
            Guid patientId,
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Verify user can only access their own appointments
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId) && userId != patientId)
            {
                return Forbid();
            }

            try
            {
                var result = await _appointmentService.GetPatientAppointmentsAsync(
                    patientId, status, pageNumber, pageSize, cancellationToken);
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
                return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
            }
        }

        /// <summary>
        /// GET: api/appointments/{appointmentId}
        /// Get appointment details (Patient or Organization)
        /// </summary>
        [HttpGet("{appointmentId}")]
        public async Task<ActionResult<AppointmentDetailDto>> GetAppointment(
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                var result = await _appointmentService.GetAppointmentDetailsAsync(
                    appointmentId, requestingUserId, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while retrieving appointment details" });
            }
        }

        /// <summary>
        /// PUT: api/appointments/{appointmentId}/reschedule
        /// Reschedule an appointment (Patient only)
        /// </summary>
        [HttpPut("{appointmentId}/reschedule")]
        //[Authorize(Roles = "Patient")]
        public async Task<IActionResult> RescheduleAppointment(
            Guid appointmentId,
            [FromBody] RescheduleAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                await _appointmentService.RescheduleAppointmentAsync(
                    appointmentId, requestingUserId, dto, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while rescheduling appointment" });
            }
        }

        /// <summary>
        /// PUT: api/appointments/{appointmentId}/cancel
        /// Cancel an appointment (Patient or Organization)
        /// </summary>
        [HttpPut("{appointmentId}/cancel")]
        public async Task<IActionResult> CancelAppointment(
            Guid appointmentId,
            [FromBody] CancelAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                await _appointmentService.CancelAppointmentAsync(
                    appointmentId, requestingUserId, dto, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while cancelling appointment" });
            }
        }

        // =========================
        // Organization Endpoints
        // =========================

        /// <summary>
        /// GET: api/appointments/organization/{organizationId}?startDate=...&endDate=...&status=...&pageNumber=1&pageSize=20
        /// Get organization's appointments with filters and pagination
        /// </summary>
        [HttpGet("organization/{organizationId}")]
        //[Authorize(Roles = "Organization")]
        public async Task<ActionResult<PagedResult<AppointmentDto>>> GetOrganizationAppointments(
            Guid organizationId,
            [FromQuery] AppointmentQueryDto query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Extract authenticated user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                var result = await _appointmentService.GetOrganizationAppointmentsAsync(
                    organizationId, requestingUserId, query, pageNumber, pageSize, cancellationToken);
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
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while retrieving organization appointments" });
            }
        }

        /// <summary>
        /// GET: api/appointments/organization/{organizationId}/requests?pageNumber=1&pageSize=20
        /// Get pending appointment requests for organization with pagination
        /// </summary>
        [HttpGet("organization/{organizationId}/requests")]
        //[Authorize(Roles = "Organization")]
        public async Task<ActionResult<PagedResult<AppointmentRequestDto>>> GetPendingRequests(
            Guid organizationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Extract authenticated user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                var result = await _appointmentService.GetPendingRequestsAsync(
                    organizationId, requestingUserId, pageNumber, pageSize, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while retrieving pending requests" });
            }
        }

        /// <summary>
        /// PUT: api/appointments/{appointmentId}/confirm
        /// Confirm an appointment (Organization only)
        /// </summary>
        [HttpPut("{appointmentId}/confirm")]
        [Authorize(Roles = "Organization")]
        public async Task<ActionResult<AppointmentDto>> ConfirmAppointment(
            Guid appointmentId,
            [FromBody] ConfirmAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                var result = await _appointmentService.ConfirmAppointmentAsync(
                    appointmentId, requestingUserId, dto, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while confirming appointment" });
            }
        }

        /// <summary>
        /// PUT: api/appointments/{appointmentId}/reject
        /// Reject an appointment (Organization only)
        /// </summary>
        [HttpPut("{appointmentId}/reject")]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> RejectAppointment(
            Guid appointmentId,
            [FromBody] RejectAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                await _appointmentService.RejectAppointmentAsync(
                    appointmentId, requestingUserId, dto, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while rejecting appointment" });
            }
        }

        /// <summary>
        /// PUT: api/appointments/{appointmentId}/complete
        /// Mark appointment as completed (Organization only)
        /// </summary>
        [HttpPut("{appointmentId}/complete")]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> CompleteAppointment(
            Guid appointmentId,
            [FromBody] CompleteAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                await _appointmentService.CompleteAppointmentAsync(
                    appointmentId, requestingUserId, dto, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while completing appointment" });
            }
        }

        /// <summary>
        /// PUT: api/appointments/{appointmentId}/no-show
        /// Mark patient as no-show (Organization only)
        /// </summary>
        [HttpPut("{appointmentId}/no-show")]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> MarkNoShow(
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestingUserId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            try
            {
                await _appointmentService.MarkNoShowAsync(
                    appointmentId, requestingUserId, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = "An error occurred while marking no-show" });
            }
        }
    }
}

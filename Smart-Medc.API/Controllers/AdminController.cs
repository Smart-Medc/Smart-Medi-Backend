using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.Admin;
using Smart_Medc.Application.Interfaces.Services;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        #region Dashboard

        [HttpGet("dashboard/stats")]
        [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var result = await _adminService.GetDashboardStatsAsync();

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, data = result.Data });
        }

        #endregion

        #region Patient Management

        [HttpGet("patients/active")]
        [ProducesResponseType(typeof(List<PatientListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActivePatients()
        {
            var result = await _adminService.GetAllActivePatientsAsync();

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpGet("patients/deleted")]
        [ProducesResponseType(typeof(List<PatientListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeletedPatients()
        {
            var result = await _adminService.GetAllDeletedPatientsAsync();

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpDelete("patients/{patientId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeletePatient(Guid patientId)
        {
            // Eanble: Later
            //var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //if (string.IsNullOrEmpty(adminUserId) || !Guid.TryParse(adminUserId, out var adminGuid))
            //    return Unauthorized();

            var result = await _adminService.SoftDeletePatientAsync(patientId, Guid.NewGuid());

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        [HttpPost("patients/{patientId}/restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RestorePatient(Guid patientId)
        {
            //var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //if (string.IsNullOrEmpty(adminUserId) || !Guid.TryParse(adminUserId, out var adminGuid))
            //    return Unauthorized();

            var result = await _adminService.RestorePatientAsync(patientId, Guid.NewGuid());

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        #endregion

        #region Organization Management

        /// <summary>
        /// Get organizations by status (Pending, Approved, Rejected)
        /// </summary>
        [HttpGet("organizations/status/{status}")]
        [ProducesResponseType(typeof(List<OrganizationListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrganizationsByStatus(string status)
        {
            var result = await _adminService.GetOrganizationsByStatusAsync(status);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpGet("organizations/{organizationId}")]
        [ProducesResponseType(typeof(OrganizationDetailsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrganizationDetails(Guid organizationId)
        {
            var result = await _adminService.GetOrganizationDetailsAsync(organizationId);

            if (!result.IsSuccess)
                return NotFound(new { success = false, message = result.Message });

            return Ok(new { success = true, data = result.Data });
        }

        [HttpPost("organizations/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveOrganization([FromBody] ApproveOrganizationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //if (string.IsNullOrEmpty(adminUserId) || !Guid.TryParse(adminUserId, out var adminGuid))
            //    return Unauthorized();

            var result = await _adminService.ApproveOrganizationAsync(request, Guid.NewGuid());

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        [HttpPost("organizations/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectOrganization([FromBody] RejectOrganizationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //if (string.IsNullOrEmpty(adminUserId) || !Guid.TryParse(adminUserId, out var adminGuid))
            //    return Unauthorized();

            var result = await _adminService.RejectOrganizationAsync(request, Guid.NewGuid());

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        #endregion

        #region Organization Deletion

        /// <summary>
        /// Soft delete organization
        /// </summary>
        [HttpDelete("organizations/{organizationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SoftDeleteOrganization(Guid organizationId)
        {
            //var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //if (string.IsNullOrEmpty(adminUserId) || !Guid.TryParse(adminUserId, out var adminGuid))
            //    return Unauthorized();

            var result = await _adminService.SoftDeleteOrganizationAsync(organizationId, Guid.NewGuid());

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// Restore soft-deleted organization
        /// </summary>
        [HttpPost("organizations/{organizationId}/restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RestoreOrganization(Guid organizationId)
        {
            //var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //if (string.IsNullOrEmpty(adminUserId) || !Guid.TryParse(adminUserId, out var adminGuid))
            //    return Unauthorized();

            var result = await _adminService.RestoreOrganizationAsync(organizationId, Guid.NewGuid());

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// Get all soft-deleted organizations
        /// </summary>
        [HttpGet("organizations/deleted")]
        [ProducesResponseType(typeof(List<OrganizationListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeletedOrganizations()
        {
            var result = await _adminService.GetDeletedOrganizationsAsync();

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        #endregion
    }
}
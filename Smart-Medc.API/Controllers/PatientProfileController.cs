using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.Patient;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientProfileController : ControllerBase
    {
        private readonly IPatientProfileService _profileService;
        private readonly ILogger<PatientProfileController> _logger;

        public PatientProfileController(
            IPatientProfileService profileService,
            ILogger<PatientProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        [HttpGet("profile")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user context in GetProfile");
                return Unauthorized("Invalid user context");
            }

            var result = await _profileService.GetPatientProfileAsync(userId, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.StatusCode == 404)
                    return NotFound(new { success = false, message = result.ErrorMessage });

                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Ok(new { success = true, data = result.Data });
        }

        [HttpPut("personal-info")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePersonalInfo(
            [FromBody] UpdatePersonalInfoRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user context in UpdatePersonalInfo");
                return Unauthorized("Invalid user context");
            }

            var result = await _profileService.UpdatePersonalInfoAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.StatusCode == 404)
                    return NotFound(new { success = false, message = result.ErrorMessage });

                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Ok(new { success = true, message = "Personal information updated successfully", data = result.Data });
        }

        [HttpPost("change-photo")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ChangeProfilePhoto(
            [FromForm] ChangeProfilePhotoRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user context in ChangeProfilePhoto");
                return Unauthorized("Invalid user context");
            }

            if (request.Photo == null || request.Photo.Length == 0)
            {
                _logger.LogWarning("No photo provided for user {UserId}", userId);
                return BadRequest(new { success = false, message = "No photo file provided" });
            }

            var result = await _profileService.ChangeProfilePhotoAsync(userId, request.Photo, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.StatusCode == 404)
                    return NotFound(new { success = false, message = result.ErrorMessage });

                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Ok(new { success = true, message = result.Data?.Message, data = result.Data });
        }


        [HttpDelete("remove-photo")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveProfilePhoto(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user context in RemoveProfilePhoto");
                return Unauthorized("Invalid user context");
            }

            var result = await _profileService.RemoveProfilePhotoAsync(userId, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.StatusCode == 404)
                    return NotFound(new { success = false, message = result.ErrorMessage });

                return BadRequest(new { success = false, message = result.ErrorMessage });
            }

            return Ok(new { success = true, message = result.Data?.Message, data = result.Data });
        }


        [HttpGet("export-medical-records")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportMedicalRecords(
            [FromQuery] string format = "json",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool includeMedicalRecords = true,
            [FromQuery] bool includeMedications = true,
            [FromQuery] bool includeProfileInfo = true,
            [FromQuery] bool includeEmergencyContact = true,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized("Invalid user context");

            // export new one
            var request = new ExportMedicalRecordsRequest
            {
                Format = format,
                FromDate = fromDate,
                ToDate = toDate,
                IncludeMedicalRecords = includeMedicalRecords,
                IncludeMedications = includeMedications,
                IncludeProfileInfo = includeProfileInfo,
                IncludeEmergencyContact = includeEmergencyContact
            };

            var result = await _profileService.ExportMedicalRecordsAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return result.StatusCode == 404
                    ? NotFound(new { success = false, message = result.ErrorMessage })
                    : BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new { success = true, data = result.Data });
        }

        [HttpGet("download-medical-records")] // This will be used.
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadMedicalRecords(
            [FromQuery] string format = "json",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool includeMedicalRecords = true,
            [FromQuery] bool includeMedications = true,
            [FromQuery] bool includeProfileInfo = true,
            [FromQuery] bool includeEmergencyContact = true,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized("Invalid user context");

            var request = new ExportMedicalRecordsRequest
            {
                Format = format,
                FromDate = fromDate,
                ToDate = toDate,
                IncludeMedicalRecords = includeMedicalRecords,
                IncludeMedications = includeMedications,
                IncludeProfileInfo = includeProfileInfo,
                IncludeEmergencyContact = includeEmergencyContact
            };

            var result = await _profileService.ExportMedicalRecordsAsFileAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.StatusCode == 404
                    ? NotFound(new { success = false, message = result.ErrorMessage })
                    : result.StatusCode == 500
                        ? StatusCode(500, new { success = false, message = result.ErrorMessage })
                        : BadRequest(new { success = false, message = result.ErrorMessage });
            }

            var (fileBytes, fileName, contentType) = result.Data;

            _logger.LogInformation("Medical records downloaded by user {UserId}. File: {FileName}, Size: {Size} bytes",
                userId, fileName, fileBytes.Length);

            return File(fileBytes, contentType, fileName);
        }

        [HttpGet("notification-preferences")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNotificationPreferences(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized("Invalid user context");

            var result = await _profileService.GetNotificationPreferencesAsync(userId, cancellationToken);

            if (!result.IsSuccess)
                return result.StatusCode == 404
                    ? NotFound(new { success = false, message = result.ErrorMessage })
                    : BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new { success = true, data = result.Data });
        }

        [HttpPut("notification-preferences/{notificationType}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNotificationPreference(
            [FromRoute] string notificationType,
            [FromBody] UpdateNotificationPreferenceRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized("Invalid user context");

            request.NotificationType = notificationType;

            var result = await _profileService.UpdateNotificationPreferenceAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return result.StatusCode == 404
                    ? NotFound(new { success = false, message = result.ErrorMessage })
                    : BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new { success = true, message = "Preference updated successfully", data = result.Data });
        }

        [HttpPut("notification-preferences/batch/update")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAllNotificationPreferences(
            [FromBody] UpdateAllNotificationPreferencesRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized("Invalid user context");

            var result = await _profileService.UpdateAllNotificationPreferencesAsync(userId, request, cancellationToken);

            if (!result.IsSuccess)
                return result.StatusCode == 404
                    ? NotFound(new { success = false, message = result.ErrorMessage })
                    : BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new { success = true, message = "All preferences updated successfully", data = result.Data });
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return Guid.Empty;
        }
    }
}
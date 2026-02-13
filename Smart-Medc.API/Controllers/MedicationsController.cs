using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.Medications;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:guid}/medications")]
    public class MedicationsController : ControllerBase
    {
        private readonly IMedicationService _service;
        public MedicationsController(IMedicationService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetMedications(Guid patientId, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        {
            var result = await _service.GetMedicationsAsync(patientId, includeInactive, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("{medicationId:guid}")]
        public async Task<IActionResult> GetMedication(Guid patientId, Guid medicationId, CancellationToken ct)
        {
            var result = await _service.GetMedicationByIdAsync(patientId, medicationId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPost]
        public async Task<IActionResult> CreateMedication(Guid patientId, [FromBody] CreateMedicationDto dto, CancellationToken ct)
        {
            var result = await _service.CreateMedicationAsync(patientId, dto, ct);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetMedication), new { patientId, medicationId = result.Data!.Id }, result.Data)
                : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPut("{medicationId:guid}")]
        public async Task<IActionResult> UpdateMedication(Guid patientId, Guid medicationId, [FromBody] UpdateMedicationDto dto, CancellationToken ct)
        {
            var result = await _service.UpdateMedicationAsync(patientId, medicationId, dto, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpDelete("{medicationId:guid}")]
        public async Task<IActionResult> DeleteMedication(Guid patientId, Guid medicationId, CancellationToken ct)
        {
            var result = await _service.DeleteMedicationAsync(patientId, medicationId, ct);
            return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPost("{medicationId:guid}/reminders")]
        public async Task<IActionResult> CreateReminder(Guid patientId, Guid medicationId, [FromBody] CreateReminderDto dto, CancellationToken ct)
        {
            var result = await _service.CreateReminderAsync(patientId, medicationId, dto, ct);
            return result.IsSuccess ? Created("", result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPut("{medicationId:guid}/reminders/{reminderId:guid}")]
        public async Task<IActionResult> UpdateReminder(Guid patientId, Guid medicationId, Guid reminderId, [FromBody] UpdateReminderDto dto, CancellationToken ct)
        {
            var result = await _service.UpdateReminderAsync(patientId, medicationId, reminderId, dto, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpDelete("{medicationId:guid}/reminders/{reminderId:guid}")]
        public async Task<IActionResult> DeleteReminder(Guid patientId, Guid medicationId, Guid reminderId, CancellationToken ct)
        {
            var result = await _service.DeleteReminderAsync(patientId, medicationId, reminderId, ct);
            return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("interactions")]
        public async Task<IActionResult> CheckInteractions(Guid patientId, CancellationToken ct)
        {
            var result = await _service.CheckInteractionsAsync(patientId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }
    }
}

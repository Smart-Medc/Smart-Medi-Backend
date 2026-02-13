using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.MedicalRecord;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:guid}/medical-records")]
    // [Authorize(Roles = "Patient")] // Uncomment when auth is wired
    public class MedicalRecordsController : ControllerBase
    {
        private readonly IMedicalRecordService _service;

        public MedicalRecordsController(IMedicalRecordService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecords(Guid patientId, [FromQuery] MedicalRecordQueryDto query, CancellationToken ct)
        {
            var result = await _service.GetPatientRecordsAsync(patientId, query, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("{recordId:guid}")]
        public async Task<IActionResult> GetRecord(Guid patientId, Guid recordId, CancellationToken ct)
        {
            var result = await _service.GetRecordByIdAsync(patientId, recordId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecord(Guid patientId, [FromBody] CreateMedicalRecordDto dto, CancellationToken ct)
        {
            var result = await _service.CreateRecordAsync(patientId, dto, ct);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetRecord), new { patientId, recordId = result.Data!.Id }, result.Data)
                : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPut("{recordId:guid}")]
        public async Task<IActionResult> UpdateRecord(Guid patientId, Guid recordId, [FromBody] UpdateMedicalRecordDto dto, CancellationToken ct)
        {
            var result = await _service.UpdateRecordAsync(patientId, recordId, dto, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpDelete("{recordId:guid}")]
        public async Task<IActionResult> DeleteRecord(Guid patientId, Guid recordId, CancellationToken ct)
        {
            var result = await _service.DeleteRecordAsync(patientId, recordId, ct);
            return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPost("{recordId:guid}/documents")]
        public async Task<IActionResult> UploadDocument(Guid patientId, Guid recordId, IFormFile file, CancellationToken ct)
        {
            var result = await _service.UploadDocumentAsync(patientId, recordId, file, ct);
            return result.IsSuccess ? Created("", result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("{recordId:guid}/documents/{documentId:guid}/download")]
        public async Task<IActionResult> DownloadDocument(Guid patientId, Guid recordId, Guid documentId, CancellationToken ct)
        {
            var result = await _service.DownloadDocumentAsync(patientId, recordId, documentId, ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { error = result.ErrorMessage });

            // We need the document info for content type
            var recordResult = await _service.GetRecordByIdAsync(patientId, recordId, ct);
            var doc = recordResult.Data?.Documents.FirstOrDefault(d => d.Id == documentId);

            return File(result.Data!, doc?.ContentType ?? "application/octet-stream", doc?.OriginalFileName ?? "download");
        }

        [HttpDelete("{recordId:guid}/documents/{documentId:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid patientId, Guid recordId, Guid documentId, CancellationToken ct)
        {
            var result = await _service.DeleteDocumentAsync(patientId, recordId, documentId, ct);
            return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(Guid patientId, CancellationToken ct)
        {
            var result = await _service.GetStatisticsAsync(patientId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }
    }
}

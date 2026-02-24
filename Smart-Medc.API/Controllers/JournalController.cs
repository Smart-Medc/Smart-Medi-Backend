using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.Journal;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId:guid}/journal")]
    public class JournalController : ControllerBase
    {
        private readonly IJournalService _service;
        public JournalController(IJournalService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetEntries(Guid patientId, [FromQuery] JournalQueryDto query, CancellationToken ct)
        {
            var result = await _service.GetEntriesAsync(patientId, query, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("{entryId:guid}")]
        public async Task<IActionResult> GetEntry(Guid patientId, Guid entryId, CancellationToken ct)
        {
            var result = await _service.GetEntryByIdAsync(patientId, entryId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPost]
        public async Task<IActionResult> CreateEntry(Guid patientId, [FromBody] CreateJournalEntryDto dto, CancellationToken ct)
        {
            var result = await _service.CreateEntryAsync(patientId, dto, ct);
            return result.IsSuccess
                ? CreatedAtAction(nameof(GetEntry), new { patientId, entryId = result.Data!.Id }, result.Data)
                : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpPut("{entryId:guid}")]
        public async Task<IActionResult> UpdateEntry(Guid patientId, Guid entryId, [FromBody] UpdateJournalEntryDto dto, CancellationToken ct)
        {
            var result = await _service.UpdateEntryAsync(patientId, entryId, dto, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpDelete("{entryId:guid}")]
        public async Task<IActionResult> DeleteEntry(Guid patientId, Guid entryId, CancellationToken ct)
        {
            var result = await _service.DeleteEntryAsync(patientId, entryId, ct);
            return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        //[HttpPost("{entryId:guid}/photos")]
        //public async Task<IActionResult> UploadPhoto(Guid patientId, Guid entryId, IFormFile photo, CancellationToken ct)
        //{
        //    var result = await _service.UploadPhotoAsync(patientId, entryId, photo, ct);
        //    return result.IsSuccess ? Created("", result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        //}

        //[HttpDelete("{entryId:guid}/photos/{photoId:guid}")]
        //public async Task<IActionResult> DeletePhoto(Guid patientId, Guid entryId, Guid photoId, CancellationToken ct)
        //{
        //    var result = await _service.DeletePhotoAsync(patientId, entryId, photoId, ct);
        //    return result.IsSuccess ? NoContent() : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        //}

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(Guid patientId, [FromQuery] DateRangeDto? range, CancellationToken ct)
        {
            var result = await _service.GetStatisticsAsync(patientId, range, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("tags")]
        public async Task<IActionResult> GetTags(Guid patientId, CancellationToken ct)
        {
            var result = await _service.GetTagsAsync(patientId, ct);
            return result.IsSuccess ? Ok(result.Data) : StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportJournal(Guid patientId, [FromQuery] string format = "csv", CancellationToken ct = default)
        {
            var result = await _service.ExportJournalAsync(patientId, format, ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { error = result.ErrorMessage });

            return File(result.Data!, "text/csv", $"journal-export-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }
    }
}

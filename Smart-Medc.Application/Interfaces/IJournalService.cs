using Microsoft.AspNetCore.Http;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Journal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.Interfaces
{
    public interface IJournalService
    {
        Task<ServiceResult<PagedResult<JournalEntryDto>>> GetEntriesAsync(
            Guid patientId, JournalQueryDto query, CancellationToken ct = default);

        Task<ServiceResult<JournalEntryDetailDto>> GetEntryByIdAsync(
            Guid patientId, Guid entryId, CancellationToken ct = default);

        Task<ServiceResult<JournalEntryDto>> CreateEntryAsync(
            Guid patientId, CreateJournalEntryDto dto, CancellationToken ct = default);

        Task<ServiceResult<JournalEntryDto>> UpdateEntryAsync(
            Guid patientId, Guid entryId, UpdateJournalEntryDto dto, CancellationToken ct = default);

        Task<ServiceResult> DeleteEntryAsync(
            Guid patientId, Guid entryId, CancellationToken ct = default);

        //Task<ServiceResult<JournalPhotoDto>> UploadPhotoAsync(
        //    Guid patientId, Guid entryId, IFormFile photo, CancellationToken ct = default);

        //Task<ServiceResult> DeletePhotoAsync(
        //    Guid patientId, Guid entryId, Guid photoId, CancellationToken ct = default);

        Task<ServiceResult<JournalStatisticsDto>> GetStatisticsAsync(
            Guid patientId, DateRangeDto? range, CancellationToken ct = default);

        Task<ServiceResult<List<string>>> GetTagsAsync(
            Guid patientId, CancellationToken ct = default);

        Task<ServiceResult<byte[]>> ExportJournalAsync(
            Guid patientId, string format, CancellationToken ct = default);
    }
}

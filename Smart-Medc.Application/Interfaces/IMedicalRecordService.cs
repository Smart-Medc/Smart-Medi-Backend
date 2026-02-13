using Microsoft.AspNetCore.Http;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.MedicalRecord;


namespace Smart_Medc.Application.Interfaces
{
    public interface IMedicalRecordService
    {
        Task<ServiceResult<PagedResult<MedicalRecordDto>>> GetPatientRecordsAsync(
            Guid patientId, MedicalRecordQueryDto query, CancellationToken ct = default);

        Task<ServiceResult<MedicalRecordDetailDto>> GetRecordByIdAsync(
            Guid patientId, Guid recordId, CancellationToken ct = default);

        Task<ServiceResult<MedicalRecordDto>> CreateRecordAsync(
            Guid patientId, CreateMedicalRecordDto dto, CancellationToken ct = default);

        Task<ServiceResult<MedicalRecordDto>> UpdateRecordAsync(
            Guid patientId, Guid recordId, UpdateMedicalRecordDto dto, CancellationToken ct = default);

        Task<ServiceResult> DeleteRecordAsync(
            Guid patientId, Guid recordId, CancellationToken ct = default);

        Task<ServiceResult<DocumentDto>> UploadDocumentAsync(
            Guid patientId, Guid recordId, IFormFile file, CancellationToken ct = default);

        Task<ServiceResult<Stream>> DownloadDocumentAsync(
            Guid patientId, Guid recordId, Guid documentId, CancellationToken ct = default);

        Task<ServiceResult> DeleteDocumentAsync(
            Guid patientId, Guid recordId, Guid documentId, CancellationToken ct = default);

        Task<ServiceResult<MedicalRecordStatisticsDto>> GetStatisticsAsync(
            Guid patientId, CancellationToken ct = default);
    }
}

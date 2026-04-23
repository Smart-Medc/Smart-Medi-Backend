using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.DataSharing;

namespace Smart_Medc.Application.Interfaces
{
    public interface IDataSharingService
    {
        Task<DataShareCodeDto> GenerateShareCodeAsync(
            Guid patientId,
            GenerateShareCodeDto dto,
            CancellationToken cancellationToken = default);

        Task<PagedResult<DataShareCodeDto>> GetCodesAsync(
            Guid userId,
            string filter = "active",
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task RevokeCodeAsync(
            Guid patientId,
            Guid codeId,
            CancellationToken cancellationToken = default);

        Task<ValidateCodeResponseDto> ValidateCodeAsync(
            string code,
            string? ipAddress = null,
            string? userAgent = null,
            Guid? organizationId = null,
            CancellationToken cancellationToken = default);

        Task<PagedResult<SharedMedicalRecordDto>> GetSharedRecordsAsync(
            string code,
            string? ipAddress = null,
            string? userAgent = null,
            Guid? organizationId = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        // NEW: returns the distinct codes this organization has accessed,
        Task<PagedResult<AccessHistoryItemDto>> GetOrganizationAccessHistoryAsync(
            Guid userId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);
    }
}

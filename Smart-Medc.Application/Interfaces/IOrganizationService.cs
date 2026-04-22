using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Organization;

namespace Smart_Medc.Application.Interfaces
{
    public interface IOrganizationService
    {
        Task<PagedResult<OrganizationSearchResultDto>> SearchAsync(
            OrganizationSearchDto query,
            CancellationToken cancellationToken = default);

        Task<OrganizationDetailDto> GetOrganizationDetailsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        Task<List<AvailabilityDto>> GetMonthAvailabilityAsync(
            Guid organizationId,
            DateTime month,
            CancellationToken cancellationToken = default);

        Task<List<TimeSlotDto>> GetDailyTimeSlotsAsync(
            Guid organizationId,
            DateTime date,
            CancellationToken cancellationToken = default);

        // ADDED: get the editable weekly schedule for the org dashboard
        Task<List<GetOperatingHoursDto>> GetOperatingHoursAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        // ADDED: save the full weekly schedule from the org dashboard
        Task UpdateOperatingHoursAsync(
            Guid organizationId,
            Guid requestingUserId,
            UpdateOperatingHoursRequestDto request,
            CancellationToken cancellationToken = default);

        // ADDED: get the exception record for a specific date (null if none exists)
        Task<AvailabilityExceptionDto?> GetExceptionByDateAsync(
            Guid organizationId,
            DateTime date,
            CancellationToken cancellationToken = default);

        // ADDED: create or update the exception for a specific date
        Task<AvailabilityExceptionDto> UpsertExceptionAsync(
            Guid organizationId,
            Guid requestingUserId,
            DateTime date,
            UpsertAvailabilityExceptionDto dto,
            CancellationToken cancellationToken = default);

        // ADDED: delete the exception for a specific date (restore to weekly template)
        Task DeleteExceptionAsync(
            Guid organizationId,
            Guid requestingUserId,
            DateTime date,
            CancellationToken cancellationToken = default);
    }
}

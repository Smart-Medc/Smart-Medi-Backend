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
    }
}

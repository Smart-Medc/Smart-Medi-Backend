using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationOperatingHoursRepository : IRepository<OrganizationOperatingHours>
    {
        Task<IEnumerable<OrganizationOperatingHours>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<OrganizationOperatingHours?> GetByDayAsync(Guid organizationId, int dayOfWeek, CancellationToken cancellationToken = default);
    }
}

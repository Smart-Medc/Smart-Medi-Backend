using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationOperatingHoursRepository : IRepository<OrganizationOperatingHours>
    {
        Task<IEnumerable<OrganizationOperatingHours>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<OrganizationOperatingHours?> GetByDayAsync(Guid organizationId, int dayOfWeek, CancellationToken cancellationToken = default);

        // ADDED: fetch a single record by organizationId + DayOfWeek for upsert
        Task<OrganizationOperatingHours?> GetByOrganizationAndDayAsync(Guid organizationId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

        // ADDED: update an existing record in place
        Task UpdateAsync(OrganizationOperatingHours hours, CancellationToken cancellationToken = default);
    }
}

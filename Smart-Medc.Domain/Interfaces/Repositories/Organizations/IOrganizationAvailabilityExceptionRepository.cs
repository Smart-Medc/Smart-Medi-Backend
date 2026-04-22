using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationAvailabilityExceptionRepository : IRepository<OrganizationAvailabilityException>
    {
        Task<IEnumerable<OrganizationAvailabilityException>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<OrganizationAvailabilityException?> GetByDateAsync(Guid organizationId, DateTime date, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrganizationAvailabilityException>> GetByDateRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        // ADDED: update an existing exception record in place
        Task UpdateAsync(OrganizationAvailabilityException exception, CancellationToken cancellationToken = default);

        // ADDED: delete an exception by its ID
        Task DeleteAsync(Guid exceptionId, CancellationToken cancellationToken = default);
    }
}

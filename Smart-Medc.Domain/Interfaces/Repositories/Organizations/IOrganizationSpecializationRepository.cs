using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationSpecializationRepository : IRepository<OrganizationSpecialization>
    {
        Task<IEnumerable<OrganizationSpecialization>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<OrganizationSpecialization?> GetPrimarySpecializationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}

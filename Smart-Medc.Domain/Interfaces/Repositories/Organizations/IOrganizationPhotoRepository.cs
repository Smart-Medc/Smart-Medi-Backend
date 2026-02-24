using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationPhotoRepository : IRepository<OrganizationPhoto>
    {
        Task<IEnumerable<OrganizationPhoto>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<OrganizationPhoto?> GetFeaturedPhotoAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}

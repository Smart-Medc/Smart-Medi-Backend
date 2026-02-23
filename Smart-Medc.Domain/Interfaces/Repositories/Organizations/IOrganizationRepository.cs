using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationRepository : IRepository<Organization>
    {
        Task<Organization?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Organization?> GetByIdWithDetailsAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Organization>> GetByVerificationStatusAsync(int status, CancellationToken cancellationToken = default);
        Task<IEnumerable<Organization>> GetByTypeAsync(int organizationType, CancellationToken cancellationToken = default);
        Task<IEnumerable<Organization>> SearchOrganizationsAsync(string searchTerm, string? city = null, int? type = null, CancellationToken cancellationToken = default);
    }
}

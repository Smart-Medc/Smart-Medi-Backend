using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface ISpecializationRepository : IRepository<Specialization>
    {
        Task<IEnumerable<Specialization>> GetActiveSpecializationsAsync(CancellationToken cancellationToken = default);
        Task<Specialization?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}

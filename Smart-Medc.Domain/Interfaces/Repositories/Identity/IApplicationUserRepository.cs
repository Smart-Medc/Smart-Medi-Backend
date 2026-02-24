using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Domain.Interfaces.Repositories.Identity
{
    public interface IApplicationUserRepository : IRepository<ApplicationUser>
    {
        Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetByIdWithPatientAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetByIdWithOrganizationAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<ApplicationUser>> GetUsersByTypeAsync(int userType, CancellationToken cancellationToken = default);
    }
}

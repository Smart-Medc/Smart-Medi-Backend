using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IDoctorRepository : IRepository<Doctor>
    {
        Task<IEnumerable<Doctor>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Doctor>> GetActiveDoctorsAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<Doctor?> GetTopRatedDoctorAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}

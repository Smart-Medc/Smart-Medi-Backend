using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IConsultationFeeRepository : IRepository<ConsultationFee>
    {
        Task<IEnumerable<ConsultationFee>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConsultationFee>> GetActiveFeesAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}

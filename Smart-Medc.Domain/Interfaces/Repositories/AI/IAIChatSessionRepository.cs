using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatSessionRepository : IRepository<AIChatSession>
    {
        Task<AIChatSession?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);
        Task<List<AIChatSession>> GetByPatientIdAsync(Guid patientId, bool includeDeleted = false, CancellationToken ct = default);
    }
}

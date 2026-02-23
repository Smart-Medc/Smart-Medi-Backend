using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatSessionRepository : IRepository<AIChatSession>
    {
        Task<IEnumerable<AIChatSession>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<AIChatSession?> GetByIdWithMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AIChatSession>> GetRecentSessionsAsync(Guid patientId, int count, CancellationToken cancellationToken = default);
    }
}

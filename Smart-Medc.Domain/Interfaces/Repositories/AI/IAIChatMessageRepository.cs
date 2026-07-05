using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatMessageRepository : IRepository<AIChatMessage>
    {
        Task<List<AIChatMessage>> GetBySessionIdAsync(Guid sessionId, bool includeDeleted = false, CancellationToken ct = default);
        Task<AIChatMessage?> GetByIdWithAttachmentsAsync(Guid id, CancellationToken ct = default);
    }
}

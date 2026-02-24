using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatMessageRepository : IRepository<AIChatMessage>
    {
        Task<IEnumerable<AIChatMessage>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<AIChatMessage?> GetByIdWithAttachmentsAsync(Guid messageId, CancellationToken cancellationToken = default);
    }
}

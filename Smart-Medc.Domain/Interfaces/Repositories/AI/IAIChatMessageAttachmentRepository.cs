using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatMessageAttachmentRepository : IRepository<AIChatMessageAttachment>
    {
        Task<IEnumerable<AIChatMessageAttachment>> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default);
    }
}

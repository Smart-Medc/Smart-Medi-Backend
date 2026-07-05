using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatMessageAttachmentRepository : IRepository<AIChatMessageAttachment>
    {
        Task<List<AIChatMessageAttachment>> GetByMessageIdAsync(Guid messageId, CancellationToken ct = default);
        Task<AIChatMessageAttachment?> GetByStoragePathAsync(string storagePath, CancellationToken ct = default);
    }
}

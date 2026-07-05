using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories.AI;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.AI
{
    public class AIChatMessageAttachmentRepository : Repository<AIChatMessageAttachment>, IAIChatMessageAttachmentRepository
    {
        public AIChatMessageAttachmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<AIChatMessageAttachment>> GetByMessageIdAsync(
            Guid messageId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(a => a.MessageId == messageId)
                .ToListAsync(ct);
        }

        public async Task<AIChatMessageAttachment?> GetByStoragePathAsync(
            string storagePath, CancellationToken ct = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.StoragePath == storagePath, ct);
        }
    }
}

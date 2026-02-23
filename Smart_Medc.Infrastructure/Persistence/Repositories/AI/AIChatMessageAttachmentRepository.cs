using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories.AI;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.AI
{
    public class AIChatMessageAttachmentRepository : Repository<AIChatMessageAttachment>, IAIChatMessageAttachmentRepository
    {
        public AIChatMessageAttachmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<AIChatMessageAttachment>> GetByMessageIdAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.MessageId == messageId)
                .OrderBy(a => a.UploadedAt)
                .ToListAsync(cancellationToken);
        }
    }
}

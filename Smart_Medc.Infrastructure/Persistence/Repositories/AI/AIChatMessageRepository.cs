using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories.AI;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.AI
{
    public class AIChatMessageRepository : Repository<AIChatMessage>, IAIChatMessageRepository
    {
        public AIChatMessageRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<AIChatMessage>> GetBySessionIdAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Attachments)
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<AIChatMessage?> GetByIdWithAttachmentsAsync(
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        }
    }
}
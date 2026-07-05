using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories.AI;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.AI
{
    public class AIChatMessageRepository : Repository<AIChatMessage>, IAIChatMessageRepository
    {
        public AIChatMessageRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<AIChatMessage>> GetBySessionIdAsync(
            Guid sessionId, bool includeDeleted = false, CancellationToken ct = default)
        {
            var query = _dbSet.Where(m => m.SessionId == sessionId);

            if (!includeDeleted)
                query = query.Where(m => !m.IsDeleted);

            return await query
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<AIChatMessage?> GetByIdWithAttachmentsAsync(
            Guid id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == id, ct);
        }
    }
}
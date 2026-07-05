using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories.AI;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.AI
{
    public class AIChatSessionRepository : Repository<AIChatSession>, IAIChatSessionRepository
    {
        public AIChatSessionRepository(ApplicationDbContext context) : base(context) { }

        public async Task<AIChatSession?> GetByIdWithMessagesAsync(
            Guid id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(s => s.Messages)
                .ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<List<AIChatSession>> GetByPatientIdAsync(
            Guid patientId, bool includeDeleted = false, CancellationToken ct = default)
        {
            var query = _dbSet.Where(s => s.PatientId == patientId);

            if (!includeDeleted)
                query = query.Where(s => !s.IsDeleted);

            return await query
                .OrderByDescending(s => s.LastMessageAt ?? s.CreatedAt)
                .ToListAsync(ct);
        }
    }
}
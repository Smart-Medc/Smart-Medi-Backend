using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.AI
{
    public class AIChatSessionRepository : Repository<AIChatSession>, IAIChatSessionRepository
    {
        public AIChatSessionRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<AIChatSession>> GetByPatientIdAsync(
            Guid patientId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Messages)
                .Where(s => s.PatientId == patientId)
                .OrderByDescending(s => s.LastMessageAt ?? s.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<AIChatSession?> GetByIdWithMessagesAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                    .ThenInclude(m => m.Attachments)
                .Include(s => s.Patient)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        }

        public async Task<IEnumerable<AIChatSession>> GetRecentSessionsAsync(
            Guid patientId,
            int count,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(s => s.PatientId == patientId)
                .OrderByDescending(s => s.LastMessageAt ?? s.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }
    }
}
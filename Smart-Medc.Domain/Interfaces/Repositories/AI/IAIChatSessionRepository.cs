using Smart_Medc.Domain.Entities.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatSessionRepository : IRepository<AIChatSession>
    {
        Task<IEnumerable<AIChatSession>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
        Task<AIChatSession?> GetByIdWithMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AIChatSession>> GetRecentSessionsAsync(Guid patientId, int count, CancellationToken cancellationToken = default);
    }
}

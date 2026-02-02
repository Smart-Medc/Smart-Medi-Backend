using Smart_Medc.Domain.Entities.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatMessageRepository : IRepository<AIChatMessage>
    {
        Task<IEnumerable<AIChatMessage>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
        Task<AIChatMessage?> GetByIdWithAttachmentsAsync(Guid messageId, CancellationToken cancellationToken = default);
    }
}

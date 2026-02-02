using Smart_Medc.Domain.Entities.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.AI
{
    public interface IAIChatMessageAttachmentRepository : IRepository<AIChatMessageAttachment>
    {
        Task<IEnumerable<AIChatMessageAttachment>> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default);
    }
}

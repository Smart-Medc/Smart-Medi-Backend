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

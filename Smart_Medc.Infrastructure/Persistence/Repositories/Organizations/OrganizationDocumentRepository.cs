using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Interfaces.Repositories.Organizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Organizations
{
    public class OrganizationDocumentRepository : Repository<OrganizationDocument>, IOrganizationDocumentRepository
    {
        public OrganizationDocumentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<OrganizationDocument>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.OrganizationId == organizationId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrganizationDocument>> GetByTypeAsync(
            Guid organizationId,
            int documentType,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.OrganizationId == organizationId &&
                           (int)d.DocumentType == documentType)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrganizationDocument>> GetPendingVerificationDocumentsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Organization)
                .Where(d => d.VerificationStatus == Domain.Enums.DocumentVerificationStatus.Pending)
                .OrderBy(d => d.UploadedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
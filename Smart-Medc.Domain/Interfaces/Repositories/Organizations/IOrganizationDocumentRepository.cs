using Smart_Medc.Domain.Entities.OrganizationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface IOrganizationDocumentRepository : IRepository<OrganizationDocument>
    {
        Task<IEnumerable<OrganizationDocument>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrganizationDocument>> GetByTypeAsync(Guid organizationId, int documentType, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrganizationDocument>> GetPendingVerificationDocumentsAsync(CancellationToken cancellationToken = default);
    }
}

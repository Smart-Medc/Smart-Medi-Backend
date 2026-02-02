using Smart_Medc.Domain.Entities.OrganizationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Interfaces.Repositories.Organizations
{
    public interface ISpecializationRepository : IRepository<Specialization>
    {
        Task<IEnumerable<Specialization>> GetActiveSpecializationsAsync(CancellationToken cancellationToken = default);
        Task<Specialization?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}

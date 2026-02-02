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
    public class OrganizationRepository : Repository<Organization>, IOrganizationRepository
    {
        public OrganizationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Organization?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.User)
                .Include(o => o.Specializations)
                    .ThenInclude(s => s.Specialization)
                .FirstOrDefaultAsync(o => o.UserId == userId, cancellationToken);
        }

        public async Task<Organization?> GetByIdWithDetailsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.User)
                .Include(o => o.Specializations)
                    .ThenInclude(s => s.Specialization)
                .Include(o => o.Documents)
                .Include(o => o.OperatingHours)
                .Include(o => o.AvailabilitySlots)
                .Include(o => o.ConsultationFees)
                .Include(o => o.Photos)
                .Include(o => o.Doctors)
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
        }

        public async Task<IEnumerable<Organization>> GetByVerificationStatusAsync(
            int status,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.User)
                .Include(o => o.Specializations)
                    .ThenInclude(s => s.Specialization)
                .Where(o => (int)o.VerificationStatus == status)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Organization>> GetByTypeAsync(
            int organizationType,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.Specializations)
                    .ThenInclude(s => s.Specialization)
                .Where(o => (int)o.Type == organizationType &&
                           o.VerificationStatus == Domain.Enums.VerificationStatus.Verified)
                .OrderBy(o => o.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Organization>> SearchOrganizationsAsync(
            string searchTerm,
            string? city = null,
            int? type = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(o => o.Specializations)
                    .ThenInclude(s => s.Specialization)
                .Where(o => o.VerificationStatus == Domain.Enums.VerificationStatus.Verified);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o => o.Name.Contains(searchTerm) ||
                                        o.Specializations.Any(s => s.Specialization.Name.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(o => o.City != null && o.City.Contains(city));
            }

            if (type.HasValue)
            {
                query = query.Where(o => (int)o.Type == type.Value);
            }

            return await query
                .OrderBy(o => o.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
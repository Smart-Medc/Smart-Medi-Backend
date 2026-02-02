using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Interfaces.Repositories.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Identity
{
    public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
    {
        public ApplicationUserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<ApplicationUser?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<ApplicationUser?> GetByIdWithPatientAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<ApplicationUser?> GetByIdWithOrganizationAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.Organization)
                    .ThenInclude(o => o.Specializations)
                        .ThenInclude(s => s.Specialization)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<bool> IsEmailUniqueAsync(
            string email,
            Guid? excludeUserId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(u => u.Email == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }

        public async Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByTypeAsync(
            int userType,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(u => (int)u.UserType == userType)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync(cancellationToken);
        }
    }
}

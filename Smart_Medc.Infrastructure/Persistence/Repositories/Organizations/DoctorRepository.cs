using Microsoft.EntityFrameworkCore;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Interfaces.Repositories.Organizations;

namespace Smart_Medc.Infrastructure.Persistence.Repositories.Organizations
{
    public class DoctorRepository : Repository<Doctor>, IDoctorRepository
    {
        public DoctorRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Doctor>> GetByOrganizationIdAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.OrganizationId == organizationId)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Doctor>> GetActiveDoctorsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.OrganizationId == organizationId && d.IsActive)
                .OrderByDescending(d => d.AverageRating)
                .ToListAsync(cancellationToken);
        }

        public async Task<Doctor?> GetTopRatedDoctorAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.OrganizationId == organizationId && d.IsActive)
                .OrderByDescending(d => d.AverageRating)
                .ThenByDescending(d => d.TotalReviews)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
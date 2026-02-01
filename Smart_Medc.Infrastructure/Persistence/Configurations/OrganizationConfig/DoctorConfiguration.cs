using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Title)
                .HasMaxLength(50);

            builder.Property(d => d.Specialization)
                .HasMaxLength(200);

            builder.Property(d => d.Biography)
                .HasMaxLength(2000);

            builder.Property(d => d.PhotoUrl)
                .HasMaxLength(500);

            builder.Property(d => d.AverageRating)
                .IsRequired()
                .HasPrecision(3, 2)
                .HasDefaultValue(0);

            builder.Property(d => d.TotalReviews)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(d => d.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(d => d.CreatedAt)
                .IsRequired();

            builder.Property(d => d.UpdatedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(d => d.OrganizationId);
            builder.HasIndex(d => d.IsActive);
            builder.HasIndex(d => d.AverageRating);
            builder.HasIndex(d => new { d.OrganizationId, d.IsActive });

            // Relationships
            builder.HasOne(d => d.Organization)
                .WithMany(o => o.Doctors)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
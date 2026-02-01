using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class OrganizationSpecializationConfiguration : IEntityTypeConfiguration<OrganizationSpecialization>
    {
        public void Configure(EntityTypeBuilder<OrganizationSpecialization> builder)
        {
            builder.HasKey(os => os.Id);

            builder.Property(os => os.IsPrimary)
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(os => os.OrganizationId);
            builder.HasIndex(os => os.SpecializationId);
            builder.HasIndex(os => new { os.OrganizationId, os.SpecializationId }).IsUnique();
            builder.HasIndex(os => new { os.OrganizationId, os.IsPrimary });

            // Relationships
            builder.HasOne(os => os.Organization)
                .WithMany(o => o.Specializations)
                .HasForeignKey(os => os.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(os => os.Specialization)
                .WithMany(s => s.OrganizationSpecializations)
                .HasForeignKey(os => os.SpecializationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
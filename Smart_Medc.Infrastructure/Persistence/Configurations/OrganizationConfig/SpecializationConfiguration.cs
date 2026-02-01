using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class SpecializationConfiguration : IEntityTypeConfiguration<Specialization>
    {
        public void Configure(EntityTypeBuilder<Specialization> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.Property(s => s.IconName)
                .HasMaxLength(50);

            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            builder.HasIndex(s => s.Name).IsUnique();
            builder.HasIndex(s => s.IsActive);

            // Relationships
            builder.HasMany(s => s.OrganizationSpecializations)
                .WithOne(os => os.Specialization)
                .HasForeignKey(os => os.SpecializationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
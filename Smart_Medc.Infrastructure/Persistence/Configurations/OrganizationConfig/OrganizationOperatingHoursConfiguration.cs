using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class OrganizationOperatingHoursConfiguration : IEntityTypeConfiguration<OrganizationOperatingHours>
    {
        public void Configure(EntityTypeBuilder<OrganizationOperatingHours> builder)
        {
            builder.HasKey(oh => oh.Id);

            builder.Property(oh => oh.DayOfWeek)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(oh => oh.IsOpen)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(oh => oh.OpenTime)
                .IsRequired(false);

            builder.Property(oh => oh.CloseTime)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(oh => oh.OrganizationId);
            builder.HasIndex(oh => new { oh.OrganizationId, oh.DayOfWeek }).IsUnique();

            // Relationships
            builder.HasOne(oh => oh.Organization)
                .WithMany(o => o.OperatingHours)
                .HasForeignKey(oh => oh.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
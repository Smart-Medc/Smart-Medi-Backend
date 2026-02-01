using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class OrganizationAvailabilitySlotConfiguration : IEntityTypeConfiguration<OrganizationAvailabilitySlot>
    {
        public void Configure(EntityTypeBuilder<OrganizationAvailabilitySlot> builder)
        {
            builder.HasKey(asl => asl.Id);

            builder.Property(asl => asl.DayOfWeek)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(asl => asl.StartTime)
                .IsRequired();

            builder.Property(asl => asl.EndTime)
                .IsRequired();

            builder.Property(asl => asl.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(asl => asl.CreatedAt)
                .IsRequired();

            builder.Property(asl => asl.UpdatedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(asl => asl.OrganizationId);
            builder.HasIndex(asl => asl.DayOfWeek);
            builder.HasIndex(asl => asl.IsEnabled);
            builder.HasIndex(asl => new { asl.OrganizationId, asl.DayOfWeek, asl.IsEnabled });

            // Relationships
            builder.HasOne(asl => asl.Organization)
                .WithMany(o => o.AvailabilitySlots)
                .HasForeignKey(asl => asl.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
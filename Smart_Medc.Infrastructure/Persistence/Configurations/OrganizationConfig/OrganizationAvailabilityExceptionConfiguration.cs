using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class OrganizationAvailabilityExceptionConfiguration : IEntityTypeConfiguration<OrganizationAvailabilityException>
    {
        public void Configure(EntityTypeBuilder<OrganizationAvailabilityException> builder)
        {
            builder.HasKey(ae => ae.Id);

            builder.Property(ae => ae.Date)
                .IsRequired();

            builder.Property(ae => ae.IsFullDayOff)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(ae => ae.StartTime)
                .IsRequired(false);

            builder.Property(ae => ae.EndTime)
                .IsRequired(false);

            builder.Property(ae => ae.Reason)
                .HasMaxLength(500);

            builder.Property(ae => ae.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(ae => ae.OrganizationId);
            builder.HasIndex(ae => ae.Date);
            builder.HasIndex(ae => new { ae.OrganizationId, ae.Date });

            // Relationships
            builder.HasOne(ae => ae.Organization)
                .WithMany()
                .HasForeignKey(ae => ae.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
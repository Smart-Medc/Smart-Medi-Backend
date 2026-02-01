using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class ConsultationFeeConfiguration : IEntityTypeConfiguration<ConsultationFee>
    {
        public void Configure(EntityTypeBuilder<ConsultationFee> builder)
        {
            builder.HasKey(cf => cf.Id);

            builder.Property(cf => cf.FeeType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(cf => cf.MinAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(cf => cf.MaxAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(cf => cf.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("USD");

            builder.Property(cf => cf.DurationMinutes)
                .IsRequired();

            builder.Property(cf => cf.Description)
                .HasMaxLength(500);

            builder.Property(cf => cf.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(cf => cf.CreatedAt)
                .IsRequired();

            builder.Property(cf => cf.UpdatedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(cf => cf.OrganizationId);
            builder.HasIndex(cf => cf.IsActive);
            builder.HasIndex(cf => new { cf.OrganizationId, cf.IsActive });

            // Relationships
            builder.HasOne(cf => cf.Organization)
                .WithMany(o => o.ConsultationFees)
                .HasForeignKey(cf => cf.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
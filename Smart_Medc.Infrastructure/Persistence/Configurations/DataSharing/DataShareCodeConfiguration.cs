using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.DataSharing;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.DataSharing
{
    public class DataShareCodeConfiguration : IEntityTypeConfiguration<DataShareCode>
    {
        public void Configure(EntityTypeBuilder<DataShareCode> builder)
        {
            builder.HasKey(dsc => dsc.Id);

            builder.Property(dsc => dsc.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(dsc => dsc.ShareUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(dsc => dsc.ExpirationType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(dsc => dsc.ExpiresAt)
                .IsRequired(false);

            builder.Property(dsc => dsc.MaxAccessCount)
                .IsRequired(false);

            builder.Property(dsc => dsc.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.DataShareStatus.Active);

            builder.Property(dsc => dsc.CreatedAt)
                .IsRequired();

            builder.Property(dsc => dsc.RevokedAt)
                .IsRequired(false);

            builder.Property(dsc => dsc.RevokedReason)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(dsc => dsc.Code).IsUnique();
            builder.HasIndex(dsc => dsc.PatientId);
            builder.HasIndex(dsc => dsc.Status);
            builder.HasIndex(dsc => dsc.ExpiresAt);
            builder.HasIndex(dsc => new { dsc.PatientId, dsc.Status });
            builder.HasIndex(dsc => dsc.CreatedAt);

            // Relationships
            builder.HasOne(dsc => dsc.Patient)
                .WithMany(p => p.DataShareCodes)
                .HasForeignKey(dsc => dsc.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(dsc => dsc.RecordAccesses)
                .WithOne(ra => ra.DataShareCode)
                .HasForeignKey(ra => ra.DataShareCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(dsc => dsc.AccessLogs)
                .WithOne(al => al.DataShareCode)
                .HasForeignKey(al => al.DataShareCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
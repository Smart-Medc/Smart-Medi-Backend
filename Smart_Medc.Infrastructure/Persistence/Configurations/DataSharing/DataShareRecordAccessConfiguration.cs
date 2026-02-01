using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.DataSharing;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.DataSharing
{
    public class DataShareRecordAccessConfiguration : IEntityTypeConfiguration<DataShareRecordAccess>
    {
        public void Configure(EntityTypeBuilder<DataShareRecordAccess> builder)
        {
            builder.HasKey(ra => ra.Id);

            builder.Property(ra => ra.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(ra => ra.DataShareCodeId);
            builder.HasIndex(ra => ra.MedicalRecordId);
            builder.HasIndex(ra => new { ra.DataShareCodeId, ra.MedicalRecordId });
            builder.HasIndex(ra => ra.CreatedAt);

            // Relationships
            builder.HasOne(ra => ra.DataShareCode)
                .WithMany(dsc => dsc.RecordAccesses)
                .HasForeignKey(ra => ra.DataShareCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ra => ra.MedicalRecord)
                .WithMany(mr => mr.SharedAccesses)
                .HasForeignKey(ra => ra.MedicalRecordId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
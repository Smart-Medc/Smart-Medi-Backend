using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.PatientConfig
{
    public class MedicalRecordDocumentConfiguration : IEntityTypeConfiguration<MedicalRecordDocument>
    {
        public void Configure(EntityTypeBuilder<MedicalRecordDocument> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.OriginalFileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.Format)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(d => d.FileSizeBytes)
                .IsRequired();

            builder.Property(d => d.StoragePath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(d => d.ThumbnailPath)
                .HasMaxLength(1000);

            builder.Property(d => d.UploadedAt)
                .IsRequired();

            builder.Property(d => d.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(d => d.DeletedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(d => d.MedicalRecordId);
            builder.HasIndex(d => d.IsDeleted);
            builder.HasIndex(d => d.UploadedAt);

            // Relationships
            builder.HasOne(d => d.MedicalRecord)
                .WithMany(mr => mr.Documents)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.PatientConfig
{
    public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
    {
        public void Configure(EntityTypeBuilder<MedicalRecord> builder)
        {
            builder.HasKey(mr => mr.Id);

            builder.Property(mr => mr.Title)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(mr => mr.Description)
                .HasMaxLength(2000);

            builder.Property(mr => mr.RecordType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(mr => mr.RecordDate)
                .IsRequired();

            builder.Property(mr => mr.ProviderName)
                .HasMaxLength(200);

            builder.Property(mr => mr.OrderedBy)
                .HasMaxLength(200);

            builder.Property(mr => mr.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.RecordStatus.Final);

            builder.Property(mr => mr.FindingsSummary)
                .HasMaxLength(4000);

            builder.Property(mr => mr.CreatedAt)
                .IsRequired();

            builder.Property(mr => mr.UpdatedAt)
                .IsRequired(false);

            builder.Property(mr => mr.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(mr => mr.DeletedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(mr => mr.PatientId);
            builder.HasIndex(mr => mr.RecordType);
            builder.HasIndex(mr => mr.RecordDate);
            builder.HasIndex(mr => mr.CreatedAt);
            builder.HasIndex(mr => mr.IsDeleted);
            builder.HasIndex(mr => new { mr.PatientId, mr.IsDeleted });

            // Relationships
            builder.HasOne(mr => mr.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(mr => mr.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(mr => mr.Documents)
                .WithOne(d => d.MedicalRecord)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(mr => mr.SharedAccesses)
                .WithOne(sa => sa.MedicalRecord)
                .HasForeignKey(sa => sa.MedicalRecordId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
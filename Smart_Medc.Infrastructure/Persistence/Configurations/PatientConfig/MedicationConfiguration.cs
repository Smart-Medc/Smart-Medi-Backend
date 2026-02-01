using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.PatientConfig
{
    public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
    {
        public void Configure(EntityTypeBuilder<Medication> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.Dosage)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.Frequency)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.Route)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(m => m.Instructions)
                .HasMaxLength(1000);

            builder.Property(m => m.StartDate)
                .IsRequired();

            builder.Property(m => m.EndDate)
                .IsRequired(false);

            builder.Property(m => m.PrescribingDoctor)
                .HasMaxLength(200);

            builder.Property(m => m.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.MedicationStatus.Active);

            builder.Property(m => m.HasInteraction)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(m => m.InteractionNotes)
                .HasMaxLength(1000);

            builder.Property(m => m.CreatedAt)
                .IsRequired();

            builder.Property(m => m.UpdatedAt)
                .IsRequired(false);

            builder.Property(m => m.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(m => m.DeletedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(m => m.PatientId);
            builder.HasIndex(m => m.Status);
            builder.HasIndex(m => m.IsDeleted);
            builder.HasIndex(m => new { m.PatientId, m.IsDeleted, m.Status });
            builder.HasIndex(m => m.StartDate);

            // Relationships
            builder.HasOne(m => m.Patient)
                .WithMany(p => p.Medications)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.Reminders)
                .WithOne(r => r.Medication)
                .HasForeignKey(r => r.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.AdherenceLogs)
                .WithOne(al => al.Medication)
                .HasForeignKey(al => al.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
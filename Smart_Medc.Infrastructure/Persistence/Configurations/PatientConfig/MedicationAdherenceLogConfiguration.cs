using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.PatientConfig
{
    public class MedicationAdherenceLogConfiguration : IEntityTypeConfiguration<MedicationAdherenceLog>
    {
        public void Configure(EntityTypeBuilder<MedicationAdherenceLog> builder)
        {
            builder.HasKey(al => al.Id);

            builder.Property(al => al.ScheduledTime)
                .IsRequired();

            builder.Property(al => al.TakenTime)
                .IsRequired(false);

            builder.Property(al => al.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(al => al.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(al => al.MedicationId);
            builder.HasIndex(al => al.ScheduledTime);
            builder.HasIndex(al => al.Status);
            builder.HasIndex(al => new { al.MedicationId, al.ScheduledTime });

            // Relationships
            builder.HasOne(al => al.Medication)
                .WithMany(m => m.AdherenceLogs)
                .HasForeignKey(al => al.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
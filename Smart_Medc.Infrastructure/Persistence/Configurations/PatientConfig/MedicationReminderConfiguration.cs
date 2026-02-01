using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.PatientConfig
{
    public class MedicationReminderConfiguration : IEntityTypeConfiguration<MedicationReminder>
    {
        public void Configure(EntityTypeBuilder<MedicationReminder> builder)
        {
            builder.HasKey(mr => mr.Id);

            builder.Property(mr => mr.ReminderTime)
                .IsRequired();

            builder.Property(mr => mr.DaysOfWeek)
                .HasMaxLength(200);

            builder.Property(mr => mr.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(mr => mr.CreatedAt)
                .IsRequired();

            builder.Property(mr => mr.UpdatedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(mr => mr.MedicationId);
            builder.HasIndex(mr => mr.IsActive);
            builder.HasIndex(mr => new { mr.MedicationId, mr.IsActive });

            // Relationships
            builder.HasOne(mr => mr.Medication)
                .WithMany(m => m.Reminders)
                .HasForeignKey(mr => mr.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
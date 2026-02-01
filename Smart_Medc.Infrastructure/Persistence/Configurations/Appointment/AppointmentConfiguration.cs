using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Appointment
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Domain.Entities.AppointmentModels.Appointment>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.AppointmentModels.Appointment> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.AppointmentNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.AppointmentDate)
                .IsRequired();

            builder.Property(a => a.StartTime)
                .IsRequired();

            builder.Property(a => a.EndTime)
                .IsRequired();

            builder.Property(a => a.DurationMinutes)
                .IsRequired();

            builder.Property(a => a.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(a => a.ReasonForVisit)
                .HasMaxLength(1000);

            builder.Property(a => a.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.AppointmentStatus.Pending);

            builder.Property(a => a.IsRecordsShared)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(a => a.RescheduleCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(a => a.CancellationReason)
                .HasMaxLength(1000);

            builder.Property(a => a.CancelledAt)
                .IsRequired(false);

            builder.Property(a => a.CompletedAt)
                .IsRequired(false);

            builder.Property(a => a.CompletionNotes)
                .HasMaxLength(2000);

            builder.Property(a => a.PreparationInstructions)
                .HasMaxLength(2000);

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            builder.Property(a => a.UpdatedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(a => a.AppointmentNumber).IsUnique();
            builder.HasIndex(a => a.PatientId);
            builder.HasIndex(a => a.OrganizationId);
            builder.HasIndex(a => a.DoctorId);
            builder.HasIndex(a => a.AppointmentDate);
            builder.HasIndex(a => a.Status);
            builder.HasIndex(a => new { a.PatientId, a.Status });
            builder.HasIndex(a => new { a.OrganizationId, a.Status });
            builder.HasIndex(a => new { a.OrganizationId, a.AppointmentDate });
            builder.HasIndex(a => a.CreatedAt);

            // Relationships
            builder.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict to avoid cascade cycles

            builder.HasOne(a => a.Organization)
                .WithMany(o => o.Appointments)
                .HasForeignKey(a => a.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict); // Changed from SetNull to NoAction to avoid cascade cycles

            builder.HasOne(a => a.DataShareCode)
                .WithMany()
                .HasForeignKey(a => a.DataShareCodeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(a => a.Reminders)
                .WithOne(r => r.Appointment)
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(a => a.StatusHistory)
                .WithOne(sh => sh.Appointment)
                .HasForeignKey(sh => sh.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
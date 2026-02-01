using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.AppointmentModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Appointment
{
    public class AppointmentReminderConfiguration : IEntityTypeConfiguration<AppointmentReminder>
    {
        public void Configure(EntityTypeBuilder<AppointmentReminder> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.ReminderType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(r => r.MinutesBefore)
                .IsRequired();

            builder.Property(r => r.IsSent)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(r => r.SentAt)
                .IsRequired(false);

            builder.Property(r => r.ScheduledFor)
                .IsRequired();

            builder.Property(r => r.Channel)
                .IsRequired()
                .HasConversion<int>();

            // Indexes
            builder.HasIndex(r => r.AppointmentId);
            builder.HasIndex(r => r.IsSent);
            builder.HasIndex(r => r.ScheduledFor);
            builder.HasIndex(r => new { r.IsSent, r.ScheduledFor });

            // Relationships
            builder.HasOne(r => r.Appointment)
                .WithMany(a => a.Reminders)
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
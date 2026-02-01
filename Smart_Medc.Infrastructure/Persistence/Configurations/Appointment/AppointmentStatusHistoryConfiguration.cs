using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.AppointmentModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Appointment
{
    public class AppointmentStatusHistoryConfiguration : IEntityTypeConfiguration<AppointmentStatusHistory>
    {
        public void Configure(EntityTypeBuilder<AppointmentStatusHistory> builder)
        {
            builder.HasKey(sh => sh.Id);

            builder.Property(sh => sh.FromStatus)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(sh => sh.ToStatus)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(sh => sh.Reason)
                .HasMaxLength(1000);

            builder.Property(sh => sh.ChangedByUserId)
                .IsRequired(false);

            builder.Property(sh => sh.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(sh => sh.AppointmentId);
            builder.HasIndex(sh => sh.CreatedAt);
            builder.HasIndex(sh => new { sh.AppointmentId, sh.CreatedAt });

            // Relationships
            builder.HasOne(sh => sh.Appointment)
                .WithMany(a => a.StatusHistory)
                .HasForeignKey(sh => sh.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
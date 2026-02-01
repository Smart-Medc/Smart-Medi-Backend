using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.Notification;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Notification
{
    public class PatientNotificationConfiguration : IEntityTypeConfiguration<PatientNotification>
    {
        public void Configure(EntityTypeBuilder<PatientNotification> builder)
        {
            builder.HasKey(pn => pn.Id);

            builder.Property(pn => pn.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(pn => pn.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(pn => pn.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(pn => pn.ActionUrl)
                .HasMaxLength(500);

            builder.Property(pn => pn.Data)
                .HasColumnType("nvarchar(max)");

            builder.Property(pn => pn.Priority)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.NotificationPriority.Normal);

            builder.Property(pn => pn.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(pn => pn.ReadAt)
                .IsRequired(false);

            builder.Property(pn => pn.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(pn => pn.PatientId);
            builder.HasIndex(pn => pn.Type);
            builder.HasIndex(pn => pn.IsRead);
            builder.HasIndex(pn => pn.Priority);
            builder.HasIndex(pn => pn.CreatedAt);
            builder.HasIndex(pn => new { pn.PatientId, pn.IsRead });
            builder.HasIndex(pn => new { pn.PatientId, pn.CreatedAt });

            // Relationships
            builder.HasOne(pn => pn.Patient)
                .WithMany(p => p.Notifications)
                .HasForeignKey(pn => pn.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
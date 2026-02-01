using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.Notification;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Notification
{
    public class OrganizationNotificationConfiguration : IEntityTypeConfiguration<OrganizationNotification>
    {
        public void Configure(EntityTypeBuilder<OrganizationNotification> builder)
        {
            builder.HasKey(on => on.Id);

            builder.Property(on => on.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(on => on.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(on => on.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(on => on.ActionUrl)
                .HasMaxLength(500);

            builder.Property(on => on.Data)
                .HasColumnType("nvarchar(max)");

            builder.Property(on => on.Priority)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.NotificationPriority.Normal);

            builder.Property(on => on.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(on => on.ReadAt)
                .IsRequired(false);

            builder.Property(on => on.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(on => on.OrganizationId);
            builder.HasIndex(on => on.Type);
            builder.HasIndex(on => on.IsRead);
            builder.HasIndex(on => on.Priority);
            builder.HasIndex(on => on.CreatedAt);
            builder.HasIndex(on => new { on.OrganizationId, on.IsRead });
            builder.HasIndex(on => new { on.OrganizationId, on.CreatedAt });

            // Relationships
            builder.HasOne(on => on.Organization)
                .WithMany(o => o.Notifications)
                .HasForeignKey(on => on.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
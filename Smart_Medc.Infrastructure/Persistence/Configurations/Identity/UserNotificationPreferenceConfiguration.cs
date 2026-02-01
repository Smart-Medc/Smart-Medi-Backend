using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Identity
{
    public class UserNotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
    {
        public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
        {
            builder.HasKey(np => np.Id);

            builder.Property(np => np.NotificationType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(np => np.EmailEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(np => np.SmsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(np => np.PushEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(np => np.CreatedAt)
                .IsRequired();

            builder.Property(np => np.UpdatedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(np => np.UserId);
            builder.HasIndex(np => new { np.UserId, np.NotificationType }).IsUnique();

            // Relationships
            builder.HasOne(np => np.User)
                .WithMany(u => u.NotificationPreferences)
                .HasForeignKey(np => np.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
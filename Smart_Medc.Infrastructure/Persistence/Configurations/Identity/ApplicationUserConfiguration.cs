using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Identity
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.ProfileImageUrl)
                .HasMaxLength(500);

            builder.Property(u => u.UserType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.IsEmailVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.IsPhoneVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.IsTwoFactorEmailEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.IsTwoFactorSmsEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.IsTwoFactorAuthenticatorEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.UserType);
            builder.HasIndex(u => u.IsActive);
            builder.HasIndex(u => u.CreatedAt);

            // Relationships
            builder.HasOne(u => u.Patient)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Organization)
                .WithOne(o => o.User)
                .HasForeignKey<Organization>(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.NotificationPreferences)
                .WithOne(np => np.User)
                .HasForeignKey(np => np.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.BackupCodes)
                .WithOne(bc => bc.User)
                .HasForeignKey(bc => bc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
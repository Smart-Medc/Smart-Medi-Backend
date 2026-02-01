using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Identity
{
    public class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
    {
        public void Configure(EntityTypeBuilder<OtpVerification> builder)
        {
            builder.HasKey(otp => otp.Id);

            builder.Property(otp => otp.Code)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(otp => otp.Purpose)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(otp => otp.DeliveryMethod)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(otp => otp.ExpiresAt)
                .IsRequired();

            builder.Property(otp => otp.CreatedAt)
                .IsRequired();

            builder.Property(otp => otp.IsUsed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(otp => otp.UsedAt)
                .IsRequired(false);

            builder.Property(otp => otp.AttemptCount)
                .IsRequired()
                .HasDefaultValue(0);

            // Indexes
            builder.HasIndex(otp => otp.UserId);
            builder.HasIndex(otp => new { otp.Code, otp.Purpose });
            builder.HasIndex(otp => otp.ExpiresAt);
            builder.HasIndex(otp => otp.IsUsed);

            // Relationships
            builder.HasOne(otp => otp.User)
                .WithMany()
                .HasForeignKey(otp => otp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
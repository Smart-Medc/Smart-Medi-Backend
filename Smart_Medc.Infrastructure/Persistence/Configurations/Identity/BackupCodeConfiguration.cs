using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.Identity
{
    public class BackupCodeConfiguration : IEntityTypeConfiguration<BackupCode>
    {
        public void Configure(EntityTypeBuilder<BackupCode> builder)
        {
            builder.HasKey(bc => bc.Id);

            builder.Property(bc => bc.Code)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(bc => bc.IsUsed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(bc => bc.UsedAt)
                .IsRequired(false);

            builder.Property(bc => bc.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(bc => bc.UserId);
            builder.HasIndex(bc => new { bc.UserId, bc.IsUsed });

            // Relationships
            builder.HasOne(bc => bc.User)
                .WithMany(u => u.BackupCodes)
                .HasForeignKey(bc => bc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
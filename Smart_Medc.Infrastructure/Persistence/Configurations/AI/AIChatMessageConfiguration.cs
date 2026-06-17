using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.AI
{
    public class AIChatMessageConfiguration : IEntityTypeConfiguration<AIChatMessage>
    {
        public void Configure(EntityTypeBuilder<AIChatMessage> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.SessionId).IsRequired();
            builder.Property(m => m.Role).IsRequired().HasConversion<int>();
            builder.Property(m => m.Content).IsRequired().HasMaxLength(10000);
            builder.Property(m => m.UsedMedicalRecords).IsRequired().HasDefaultValue(false);
            builder.Property(m => m.TokensUsed).IsRequired(false);
            builder.Property(m => m.CreatedAt).IsRequired();
            builder.Property(m => m.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(m => m.DeletedAt).IsRequired(false);

            // Indexes
            builder.HasIndex(m => m.SessionId);
            builder.HasIndex(m => m.Role);
            builder.HasIndex(m => m.CreatedAt);
            builder.HasIndex(m => new { m.SessionId, m.CreatedAt });
            builder.HasIndex(m => new { m.SessionId, m.IsDeleted });

            // Relationships
            builder.HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(m => m.Attachments)
                .WithOne(a => a.Message)
                .HasForeignKey(a => a.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
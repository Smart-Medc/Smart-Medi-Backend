using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.AI
{
    public class AIChatMessageAttachmentConfiguration : IEntityTypeConfiguration<AIChatMessageAttachment>
    {
        public void Configure(EntityTypeBuilder<AIChatMessageAttachment> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.MessageId).IsRequired();
            builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
            builder.Property(a => a.StoragePath).IsRequired().HasMaxLength(1000);
            builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.FileSizeBytes).IsRequired();
            builder.Property(a => a.UploadedAt).IsRequired();

            // Indexes
            builder.HasIndex(a => a.MessageId);
            builder.HasIndex(a => a.StoragePath).IsUnique();
            builder.HasIndex(a => a.UploadedAt);

            // Relationships
            builder.HasOne(a => a.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.MessageId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        }
    }
}
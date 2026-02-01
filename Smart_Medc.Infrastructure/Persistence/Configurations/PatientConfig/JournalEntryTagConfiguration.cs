using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.PatientConfig
{
    public class JournalEntryTagConfiguration : IEntityTypeConfiguration<JournalEntryTag>
    {
        public void Configure(EntityTypeBuilder<JournalEntryTag> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Tag)
                .IsRequired()
                .HasMaxLength(50);

            // Indexes
            builder.HasIndex(t => t.JournalEntryId);
            builder.HasIndex(t => t.Tag);
            builder.HasIndex(t => new { t.JournalEntryId, t.Tag });

            // Relationships
            builder.HasOne(t => t.JournalEntry)
                .WithMany(je => je.Tags)
                .HasForeignKey(t => t.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
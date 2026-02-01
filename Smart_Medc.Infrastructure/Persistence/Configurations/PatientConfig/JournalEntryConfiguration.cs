using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.PatientModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.PatientConfig
{
    public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
    {
        public void Configure(EntityTypeBuilder<JournalEntry> builder)
        {
            builder.HasKey(je => je.Id);

            builder.Property(je => je.Title)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(je => je.Content)
                .IsRequired()
                .HasMaxLength(10000);

            builder.Property(je => je.MoodLevel)
                .IsRequired(false);

            builder.Property(je => je.PainLevel)
                .IsRequired(false);

            builder.Property(je => je.Symptom)
                .HasMaxLength(1000);

            builder.Property(je => je.EntryDate)
                .IsRequired();

            builder.Property(je => je.EntryTime)
                .IsRequired();

            builder.Property(je => je.CreatedAt)
                .IsRequired();

            builder.Property(je => je.UpdatedAt)
                .IsRequired(false);

            builder.Property(je => je.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(je => je.DeletedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(je => je.PatientId);
            builder.HasIndex(je => je.EntryDate);
            builder.HasIndex(je => je.IsDeleted);
            builder.HasIndex(je => new { je.PatientId, je.IsDeleted });
            builder.HasIndex(je => je.CreatedAt);

            // Relationships
            builder.HasOne(je => je.Patient)
                .WithMany(p => p.JournalEntries)
                .HasForeignKey(je => je.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(je => je.Tags)
                .WithOne(t => t.JournalEntry)
                .HasForeignKey(t => t.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
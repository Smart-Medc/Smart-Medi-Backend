using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.AI
{
    public class AIChatSessionConfiguration : IEntityTypeConfiguration<AIChatSession>
    {
        public void Configure(EntityTypeBuilder<AIChatSession> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.PatientId).IsRequired();
            builder.Property(s => s.Title).HasMaxLength(200);
            builder.Property(s => s.UseMedicalRecordsContext).IsRequired().HasDefaultValue(false);
            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.LastMessageAt).IsRequired(false);
            builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(s => s.DeletedAt).IsRequired(false);

            // Indexes
            builder.HasIndex(s => s.PatientId);
            builder.HasIndex(s => s.CreatedAt);
            builder.HasIndex(s => s.LastMessageAt);
            builder.HasIndex(s => new { s.PatientId, s.CreatedAt });
            builder.HasIndex(s => new { s.PatientId, s.IsDeleted });

            // Relationships
            builder.HasOne(s => s.Patient)
                .WithMany(p => p.AIChatSessions)
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
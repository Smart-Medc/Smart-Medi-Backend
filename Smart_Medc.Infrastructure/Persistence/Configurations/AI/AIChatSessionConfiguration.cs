using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.AI;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.AI
{
    public class AIChatSessionConfiguration : IEntityTypeConfiguration<AIChatSession>
    {
        public void Configure(EntityTypeBuilder<AIChatSession> builder)
        {
            builder.HasKey(acs => acs.Id);

            builder.Property(acs => acs.Title)
                .HasMaxLength(200);

            builder.Property(acs => acs.UseMedicalRecordsContext)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(acs => acs.CreatedAt)
                .IsRequired();

            builder.Property(acs => acs.LastMessageAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(acs => acs.PatientId);
            builder.HasIndex(acs => acs.CreatedAt);
            builder.HasIndex(acs => acs.LastMessageAt);
            builder.HasIndex(acs => new { acs.PatientId, acs.CreatedAt });

            // Relationships
            builder.HasOne(acs => acs.Patient)
                .WithMany(p => p.AIChatSessions)
                .HasForeignKey(acs => acs.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(acs => acs.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
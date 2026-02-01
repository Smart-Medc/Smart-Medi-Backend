using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.PatientConfig
{
    public class PatientConfiguration : IEntityTypeConfiguration<Smart_Medc.Domain.Entities.PatientModels.Patient>
    {
        public void Configure(EntityTypeBuilder<Smart_Medc.Domain.Entities.PatientModels.Patient> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.DateOfBirth)
                .IsRequired();

            builder.Property(p => p.Gender)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(p => p.BloodType)
                .HasConversion<int?>()
                .IsRequired(false);

            builder.Property(p => p.Address)
                .HasMaxLength(500);

            builder.Property(p => p.Allergies)
                .HasMaxLength(2000);

            builder.Property(p => p.HasNoKnownAllergies)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.EmergencyContactName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.EmergencyContactPhone)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(p => p.EmergencyContactRelationship)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(p => p.StorageUsedBytes)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.StorageLimitBytes)
                .IsRequired()
                .HasDefaultValue(10737418240L); // 10 GB

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(p => p.UserId).IsUnique();
            builder.HasIndex(p => p.DateOfBirth);
            builder.HasIndex(p => p.CreatedAt);

            // Relationships
            builder.HasOne(p => p.User)
                .WithOne(u => u.Patient)
                .HasForeignKey<Smart_Medc.Domain.Entities.PatientModels.Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.MedicalRecords)
                .WithOne(mr => mr.Patient)
                .HasForeignKey(mr => mr.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Medications)
                .WithOne(m => m.Patient)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.JournalEntries)
                .WithOne(je => je.Patient)
                .HasForeignKey(je => je.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.DataShareCodes)
                .WithOne(dsc => dsc.Patient)
                .HasForeignKey(dsc => dsc.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Notifications)
                .WithOne(n => n.Patient)
                .HasForeignKey(n => n.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.AIChatSessions)
                .WithOne(acs => acs.Patient)
                .HasForeignKey(acs => acs.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
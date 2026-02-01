using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(o => o.Description)
                .HasMaxLength(2000);

            builder.Property(o => o.Address)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(o => o.City)
                .HasMaxLength(100);

            builder.Property(o => o.State)
                .HasMaxLength(100);

            builder.Property(o => o.ZipCode)
                .HasMaxLength(20);

            builder.Property(o => o.Country)
                .HasMaxLength(100);

            builder.Property(o => o.Website)
                .HasMaxLength(200);

            builder.Property(o => o.SSN)
                .HasMaxLength(50);

            builder.Property(o => o.VerificationStatus)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.VerificationStatus.Pending);

            builder.Property(o => o.VerifiedAt)
                .IsRequired(false);

            builder.Property(o => o.RejectionReason)
                .HasMaxLength(1000);

            builder.Property(o => o.AllowSameDayBooking)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(o => o.MinCancellationNoticeHours)
                .IsRequired()
                .HasDefaultValue(24);

            builder.Property(o => o.MaxReschedulesAllowed)
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(o => o.AutoRejectDays)
                .IsRequired()
                .HasDefaultValue(3);

            builder.Property(o => o.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(o => o.UserId).IsUnique();
            builder.HasIndex(o => o.Type);
            builder.HasIndex(o => o.VerificationStatus);
            builder.HasIndex(o => new { o.Type, o.VerificationStatus });
            builder.HasIndex(o => o.City);
            builder.HasIndex(o => o.CreatedAt);

            // Relationships
            builder.HasOne(o => o.User)
                .WithOne(u => u.Organization)
                .HasForeignKey<Organization>(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Specializations)
                .WithOne(s => s.Organization)
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Documents)
                .WithOne(d => d.Organization)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.OperatingHours)
                .WithOne(oh => oh.Organization)
                .HasForeignKey(oh => oh.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.AvailabilitySlots)
                .WithOne(asl => asl.Organization)
                .HasForeignKey(asl => asl.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.ConsultationFees)
                .WithOne(cf => cf.Organization)
                .HasForeignKey(cf => cf.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Photos)
                .WithOne(p => p.Organization)
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Doctors)
                .WithOne(d => d.Organization)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Appointments)
                .WithOne(a => a.Organization)
                .HasForeignKey(a => a.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.Notifications)
                .WithOne(n => n.Organization)
                .HasForeignKey(n => n.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
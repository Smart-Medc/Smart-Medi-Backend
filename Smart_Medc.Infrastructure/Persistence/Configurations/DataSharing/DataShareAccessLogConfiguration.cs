using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.DataSharing;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.DataSharing
{
    public class DataShareAccessLogConfiguration : IEntityTypeConfiguration<DataShareAccessLog>
    {
        public void Configure(EntityTypeBuilder<DataShareAccessLog> builder)
        {
            builder.HasKey(al => al.Id);

            builder.Property(al => al.IpAddress)
                .HasMaxLength(50);

            builder.Property(al => al.UserAgent)
                .HasMaxLength(500);

            builder.Property(al => al.Location)
                .HasMaxLength(200);

            builder.Property(al => al.AccessedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(al => al.DataShareCodeId);
            builder.HasIndex(al => al.OrganizationId);
            builder.HasIndex(al => al.AccessedAt);
            builder.HasIndex(al => new { al.DataShareCodeId, al.AccessedAt });

            // Relationships
            builder.HasOne(al => al.DataShareCode)
                .WithMany(dsc => dsc.AccessLogs)
                .HasForeignKey(al => al.DataShareCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(al => al.Organization)
                .WithMany()
                .HasForeignKey(al => al.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
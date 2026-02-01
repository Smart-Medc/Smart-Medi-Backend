using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class OrganizationPhotoConfiguration : IEntityTypeConfiguration<OrganizationPhoto>
    {
        public void Configure(EntityTypeBuilder<OrganizationPhoto> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(p => p.StoragePath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(p => p.ThumbnailPath)
                .HasMaxLength(1000);

            builder.Property(p => p.Caption)
                .HasMaxLength(200);

            builder.Property(p => p.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.IsFeatured)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.UploadedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(p => p.OrganizationId);
            builder.HasIndex(p => p.DisplayOrder);
            builder.HasIndex(p => p.IsFeatured);
            builder.HasIndex(p => new { p.OrganizationId, p.DisplayOrder });

            // Relationships
            builder.HasOne(p => p.Organization)
                .WithMany(o => o.Photos)
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
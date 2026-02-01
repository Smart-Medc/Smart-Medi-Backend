using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Infrastructure.Persistence.Configurations.OrganizationConfig
{
    public class OrganizationDocumentConfiguration : IEntityTypeConfiguration<OrganizationDocument>
    {
        public void Configure(EntityTypeBuilder<OrganizationDocument> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.DocumentName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.DocumentType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.StoragePath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(d => d.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.FileSizeBytes)
                .IsRequired();

            builder.Property(d => d.VerificationStatus)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.DocumentVerificationStatus.Pending);

            builder.Property(d => d.VerifiedAt)
                .IsRequired(false);

            builder.Property(d => d.UploadedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(d => d.OrganizationId);
            builder.HasIndex(d => d.DocumentType);
            builder.HasIndex(d => d.VerificationStatus);
            builder.HasIndex(d => new { d.OrganizationId, d.DocumentType });
            builder.HasIndex(d => d.UploadedAt);

            // Relationships
            builder.HasOne(d => d.Organization)
                .WithMany(o => o.Documents)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
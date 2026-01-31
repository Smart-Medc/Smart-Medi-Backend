using Smart_Medc.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class OrganizationDocument
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }

        public string DocumentName { get; set; } = string.Empty;
        public OrganizationDocumentType DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        public DocumentVerificationStatus VerificationStatus { get; set; } = DocumentVerificationStatus.Pending;
        public DateTime? VerifiedAt { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
    }

}

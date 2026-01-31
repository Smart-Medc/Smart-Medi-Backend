using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class OrganizationPhoto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string? ThumbnailPath { get; set; }
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsFeatured { get; set; } = false;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
    }
}

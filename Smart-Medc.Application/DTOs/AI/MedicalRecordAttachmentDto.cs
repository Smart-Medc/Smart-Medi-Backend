using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class MedicalRecordAttachmentDto
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string RecordTitle { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}

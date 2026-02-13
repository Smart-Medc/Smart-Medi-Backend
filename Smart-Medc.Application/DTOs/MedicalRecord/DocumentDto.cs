using Smart_Medc.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public DocumentFormat Format { get; set; }
        public string FormatName => Format.ToString();
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}

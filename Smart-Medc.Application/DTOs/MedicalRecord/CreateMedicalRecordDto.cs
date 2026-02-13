using Smart_Medc.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class CreateMedicalRecordDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public MedicalRecordType RecordType { get; set; }
        public DateTime RecordDate { get; set; }
        public string? ProviderName { get; set; }
        public string? OrderedBy { get; set; }
        public string? FindingsSummary { get; set; }
    }
}

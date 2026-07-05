using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Patient
{
    public class ExportMedicalRecordsRequest
    {
        [Required(ErrorMessage = "Export format is required")]
        [RegularExpression(@"^(json|pdf)$", ErrorMessage = "Format must be either 'json' or 'pdf'")]
        public string Format { get; set; } = "json";
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
        public bool IncludeMedicalRecords { get; set; } = true;
        public bool IncludeMedications { get; set; } = true;
        public bool IncludeProfileInfo { get; set; } = true;
        public bool IncludeEmergencyContact { get; set; } = true;
        public bool IncludeDocumentFiles { get; set; } = false;
    }
}
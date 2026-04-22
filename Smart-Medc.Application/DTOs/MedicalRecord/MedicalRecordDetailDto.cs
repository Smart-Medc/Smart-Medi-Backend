namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class MedicalRecordDetailDto : MedicalRecordDto
    {
        public List<DocumentDto> Documents { get; set; } = new();
    }
}

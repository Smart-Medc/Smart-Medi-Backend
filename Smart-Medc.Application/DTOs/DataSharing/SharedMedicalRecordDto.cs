
namespace Smart_Medc.Application.DTOs.DataSharing
{
    public class SharedMedicalRecordDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Doctor { get; set; } = string.Empty;
        public List<SharedDocumentDto> Documents { get; set; } = new();
    }
}

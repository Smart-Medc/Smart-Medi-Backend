
namespace Smart_Medc.Application.DTOs.DataSharing
{
    // Patient Side
    public class GenerateShareCodeDto
    {
        public List<Guid> SpecificRecordIds { get; set; } = new();
        public string ExpirationType { get; set; } = string.Empty; // Changed to string
    }
}

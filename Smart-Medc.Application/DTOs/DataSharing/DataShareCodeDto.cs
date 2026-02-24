
namespace Smart_Medc.Application.DTOs.DataSharing
{
    public class DataShareCodeDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ShareUrl { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int AccessCount { get; set; }
        public List<string> SharedRecordsSummary { get; set; } = new();
    }
}

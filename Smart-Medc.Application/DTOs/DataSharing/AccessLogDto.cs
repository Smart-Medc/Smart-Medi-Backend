
namespace Smart_Medc.Application.DTOs.DataSharing
{
    public class AccessLogDto
    {
        public DateTime AccessedAt { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string? Location { get; set; }
    }
}

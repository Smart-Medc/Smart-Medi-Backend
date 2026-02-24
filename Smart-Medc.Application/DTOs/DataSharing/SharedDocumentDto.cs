
namespace Smart_Medc.Application.DTOs.DataSharing
{
    public class SharedDocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string DownloadUrl { get; set; } = string.Empty; 
    }
}

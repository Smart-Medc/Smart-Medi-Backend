namespace Smart_Medc.Application.DTOs.Journal
{
    public class JournalEntryDetailDto : JournalEntryDto
    {
        public string Content { get; set; } = string.Empty;
        public List<JournalPhotoDto> Photos { get; set; } = new();
    }
}

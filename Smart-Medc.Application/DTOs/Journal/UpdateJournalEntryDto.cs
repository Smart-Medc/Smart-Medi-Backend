namespace Smart_Medc.Application.DTOs.Journal
{
    public class UpdateJournalEntryDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public int? MoodLevel { get; set; }
        public int? PainLevel { get; set; }
        public List<string>? Symptoms { get; set; }
        public List<string>? Tags { get; set; }
    }
}

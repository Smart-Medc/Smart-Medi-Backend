namespace Smart_Medc.Application.DTOs.Journal
{
    public class JournalQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public int? MinMood { get; set; }
        public int? MaxMood { get; set; }
        public int? MinPain { get; set; }
        public int? MaxPain { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Tag { get; set; }
        public string? SortBy { get; set; } = "EntryDate";
        public bool SortDescending { get; set; } = true;
    }
}

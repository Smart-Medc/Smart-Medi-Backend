using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.Journal
{
    public class JournalEntryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public int? MoodLevel { get; set; }
        public int? PainLevel { get; set; }
        public List<string> Symptoms { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public bool HasPhotos { get; set; }
        public int PhotoCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

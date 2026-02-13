using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.Journal
{
    public class JournalStatisticsDto
    {
        public int TotalEntries { get; set; }
        public int EntriesThisMonth { get; set; }
        public double AverageMood { get; set; }
        public double AveragePain { get; set; }
        public List<string> MostCommonSymptoms { get; set; } = new();
        public List<string> MostUsedTags { get; set; } = new();
    }
}

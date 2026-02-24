using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.Journal
{
    public class JournalEntryDetailDto : JournalEntryDto
    {
        public string Content { get; set; } = string.Empty;
        public List<JournalPhotoDto> Photos { get; set; } = new();
    }
}

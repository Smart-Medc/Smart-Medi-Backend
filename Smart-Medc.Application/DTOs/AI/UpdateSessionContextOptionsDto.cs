using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class UpdateSessionContextOptionsDto
    {
        public bool UseMedicalRecordsContext { get; set; }

        public bool IncludeMedicalRecords { get; set; } = true;
        public bool IncludeCurrentMedications { get; set; } = false;
        public bool IncludePastMedications { get; set; } = false;
        public bool IncludeJournalEntries { get; set; } = false;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.Medications
{
    public class InteractionDto
    {
        public Guid MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string? InteractionNotes { get; set; }
    }
}

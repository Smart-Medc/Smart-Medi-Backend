using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.Medications
{
    public class MedicationDetailDto : MedicationDto
    {
        public List<ReminderDto> Reminders { get; set; } = new();
    }
}

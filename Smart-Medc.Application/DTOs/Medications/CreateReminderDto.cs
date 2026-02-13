using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.Medications
{
    public class CreateReminderDto
    {
        public TimeOnly ReminderTime { get; set; }
        public string? DaysOfWeek { get; set; }
    }
}

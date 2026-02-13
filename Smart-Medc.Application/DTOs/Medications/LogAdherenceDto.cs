using Smart_Medc.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.Medications
{
    public class LogAdherenceDto
    {
        public DateTime ScheduledTime { get; set; }
        public DateTime? TakenTime { get; set; }
        public AdherenceStatus Status { get; set; }
    }
}

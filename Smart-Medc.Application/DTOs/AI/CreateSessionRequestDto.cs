using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class CreateSessionRequestDto
    {
        public string? Title { get; set; }
        public bool UseMedicalRecordsContext { get; set; } = false;
    }
}

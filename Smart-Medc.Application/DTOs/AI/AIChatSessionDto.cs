using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class AIChatSessionDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public bool UseMedicalRecordsContext { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int MessageCount { get; set; }
    }
}

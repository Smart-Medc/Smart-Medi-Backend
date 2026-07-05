using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class AIChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string Role { get; set; } = "user";   // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public bool UsedMedicalRecords { get; set; }
        public int? TokensUsed { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AIChatAttachmentDto> Attachments { get; set; } = new();
    }
}

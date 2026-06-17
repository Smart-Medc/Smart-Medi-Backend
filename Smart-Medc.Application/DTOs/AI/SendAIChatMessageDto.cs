using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class SendAIChatMessageDto
    {
        public Guid SessionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool UseMedicalRecordsContext { get; set; }
        public List<Guid> AttachmentIds { get; set; } = new(); // Already uploaded files from R2
    }
}

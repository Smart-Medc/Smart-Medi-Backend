using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class AIChatStreamingChunkDto
    {
        public Guid MessageId { get; set; }
        public string Chunk { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public int? TokensUsed { get; set; } // When complete
    }
}

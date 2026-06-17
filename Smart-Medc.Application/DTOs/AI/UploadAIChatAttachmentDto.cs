using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class UploadAIChatAttachmentDto
    {
        public Guid SessionId { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.AI
{
    public class SendMessageRequestDto
    {
        public string Content { get; set; } = string.Empty;
        // Files will be sent as multipart/form-data, not in JSON body
    }
}

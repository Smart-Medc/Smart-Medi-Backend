using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.Interfaces
{
    public interface IChatHubDispatcher
    {
        Task SendTokenAsync(string connectionId, string token, CancellationToken ct);
        Task SendCompletionAsync(string connectionId, Guid messageId, CancellationToken ct);
        Task SendErrorAsync(string connectionId, string errorMessage, CancellationToken ct);
    }
}

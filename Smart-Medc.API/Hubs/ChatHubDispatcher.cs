using Microsoft.AspNetCore.SignalR;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.API.Hubs
{
    public class ChatHubDispatcher : IChatHubDispatcher
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatHubDispatcher(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendTokenAsync(string connectionId, string token, CancellationToken ct)
        {
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveToken", token, ct);
        }

        public async Task SendCompletionAsync(string connectionId, Guid messageId, CancellationToken ct)
        {
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveCompletion", messageId, ct);
        }

        public async Task SendErrorAsync(string connectionId, string errorMessage, CancellationToken ct)
        {
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ReceiveError", errorMessage, ct);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Smart_Medc.Application.DTOs.AI;
using Smart_Medc.Application.Interfaces;
using System.Security.Claims;

namespace Smart_Medc.API.Hubs
{
    [Authorize(Roles = "Patient")]
    public class ChatHub : Hub
    {
        // Can be used to manually associate user to connection if needed
        public override async Task OnConnectedAsync()
        {
            // Context.User.Identity.Name etc.
            await base.OnConnectedAsync();
        }
    }
}
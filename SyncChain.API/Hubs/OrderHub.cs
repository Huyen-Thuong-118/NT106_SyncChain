using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SyncChain.API.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("user_id")?.Value;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "";
        if (role is "staff" or "manager" or "admin")
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");

        await base.OnConnectedAsync();
    }

    public async Task JoinUserGroup()
    {
        var userId = Context.User?.FindFirst("user_id")?.Value;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task JoinStaffGroup()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "";
        if (role is "staff" or "manager" or "admin")
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
    }
}

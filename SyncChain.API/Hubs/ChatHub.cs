using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SyncChain.API.Hubs;

[Authorize(Policy = "InternalOnly")]
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetRequiredUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    public async Task StartCall(int receiverId, string callId)
    {
        var callerId = GetRequiredUserId();
        if (callerId == receiverId)
            return;

        await Clients.Group(UserGroup(receiverId)).SendAsync("IncomingCall", new
        {
            callId,
            callerId
        });
    }

    public async Task AcceptCall(int callerId, string callId)
    {
        var receiverId = GetRequiredUserId();
        await Clients.Group(UserGroup(callerId)).SendAsync("CallAccepted", new
        {
            callId,
            receiverId
        });
    }

    public async Task RejectCall(int callerId, string callId)
    {
        var receiverId = GetRequiredUserId();
        await Clients.Group(UserGroup(callerId)).SendAsync("CallRejected", new
        {
            callId,
            receiverId
        });
    }

    public async Task EndCall(int otherUserId, string callId)
    {
        var userId = GetRequiredUserId();
        await Clients.Group(UserGroup(otherUserId)).SendAsync("CallEnded", new
        {
            callId,
            userId
        });
    }

    public async Task Busy(int callerId, string callId)
    {
        var receiverId = GetRequiredUserId();
        await Clients.Group(UserGroup(callerId)).SendAsync("CallBusy", new
        {
            callId,
            receiverId
        });
    }

    public async Task SendCallSignal(int receiverId, string callId, string signalType, string payload)
    {
        var senderId = GetRequiredUserId();
        await Clients.Group(UserGroup(receiverId)).SendAsync("CallSignal", new
        {
            callId,
            senderId,
            signalType,
            payload
        });
    }

    public static string UserGroup(int userId) => $"user:{userId}";

    private int GetRequiredUserId()
    {
        var value = Context.User?.FindFirst("user_id")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(value, out var userId))
            throw new HubException("Yeu cau chua duoc xac thuc.");

        return userId;
    }
}

using Microsoft.AspNetCore.SignalR.Client;

namespace SyncChain.Desktop.Services;

public class SignalRService
{
    private const string HubUrl = "http://localhost:5292/hubs/order";
    private HubConnection? _conn;

    // Handlers phía MAUI đăng ký
    public event Action<int, string, string>? OnOrderStatusUpdated;
    public event Action<int, bool, string, string>? OnPaymentResult;
    public event Action<string, string>? OnNewNotification;

    public HubConnectionState State =>
        _conn?.State ?? HubConnectionState.Disconnected;

    public async Task StartAsync(string token)
    {
        if (_conn != null)
            await _conn.DisposeAsync();

        _conn = new HubConnectionBuilder()
            .WithUrl(HubUrl, opts =>
            {
                opts.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _conn.On<int, string, string>("OrderStatusUpdated", (orderId, status, ts) =>
            MainThread.BeginInvokeOnMainThread(() =>
                OnOrderStatusUpdated?.Invoke(orderId, status, ts)));

        _conn.On<int, bool, string, string>("PaymentResult", (orderId, success, method, ts) =>
            MainThread.BeginInvokeOnMainThread(() =>
                OnPaymentResult?.Invoke(orderId, success, method, ts)));

        _conn.On<string, string>("NewNotification", (title, content) =>
            MainThread.BeginInvokeOnMainThread(() =>
                OnNewNotification?.Invoke(title, content)));

        try
        {
            await _conn.StartAsync();

            await _conn.InvokeAsync("JoinUserGroup");
            var role = TokenStore.Role ?? "";
            if (role is "staff" or "manager" or "admin")
                await _conn.InvokeAsync("JoinStaffGroup");

            System.Diagnostics.Debug.WriteLine(
                $"[SignalR] Connected. State={_conn.State}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] Connect failed: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (_conn == null) return;
        try { await _conn.StopAsync(); }
        catch { }
        await _conn.DisposeAsync();
        _conn = null;
    }
}

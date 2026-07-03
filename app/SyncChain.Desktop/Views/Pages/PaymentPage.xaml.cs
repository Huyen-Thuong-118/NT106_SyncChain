using System.Net.Http.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(OrderId),  "orderId")]
[QueryProperty(nameof(Amount),   "amount")]
[QueryProperty(nameof(OrderCode),"orderCode")]
public partial class PaymentPage : ContentPage
{
    private readonly HttpClient _http;
    private int    _orderId;
    private decimal _amount;
    private string  _orderCode = string.Empty;
    private string  _selectedMethod = "cod";
    private bool    _waitingForSignalR;

    public int     OrderId   { get => _orderId;   set { _orderId   = value; RefreshLabels(); } }
    public string  Amount    { get => _amount.ToString(); set { _ = decimal.TryParse(value, out _amount); RefreshLabels(); } }
    public string  OrderCode { get => _orderCode; set { _orderCode = value; RefreshLabels(); } }

    public PaymentPage() : this(Services.ApiClientProvider.Client) { }

    public PaymentPage(HttpClient http)
    {
        _http = http;
        InitializeComponent();
    }

    private void RefreshLabels()
    {
        if (OrderInfoLabel == null) return;
        OrderInfoLabel.Text = string.IsNullOrEmpty(_orderCode) ? $"Đơn #{_orderId}" : _orderCode;
        TotalLabel.Text     = $"{_amount:N0} đ";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshLabels();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Gỡ handler nếu đang chờ (tránh ghost subscription)
        if (_waitingForSignalR)
        {
            App.SignalR.OnPaymentResult -= OnPaymentResultReceived;
            _waitingForSignalR = false;
        }
    }

    private void OnMethodChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        if (sender == CodRadio)    _selectedMethod = "cod";
        if (sender == VnpayRadio)  _selectedMethod = "vnpay";
        if (sender == MomoRadio)   _selectedMethod = "momo";
    }

    private async void OnConfirmClicked(object? sender, EventArgs e)
    {
        ConfirmButton.IsEnabled = false;
        WaitingNotice.IsVisible = false;

        try
        {
            var resp = await _http.PostAsJsonAsync("api/payment/initiate", new
            {
                MaDonHang  = _orderId,
                PhuongThuc = _selectedMethod
            });

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadFromJsonAsync<ErrorResp>();
                await DisplayAlert("Lỗi", err?.message ?? "Không thể khởi tạo thanh toán", "OK");
                return;
            }

            var result = await resp.Content.ReadFromJsonAsync<PaymentInitResponse>();
            if (result == null) return;

            if (_selectedMethod == "cod")
            {
                await DisplayAlert("Thành công", "Đơn hàng COD đã được xác nhận!", "OK");
                await NavigateToTracking();
                return;
            }

            // VNPay / MoMo — mở trình duyệt, chờ SignalR
            if (!string.IsNullOrEmpty(result.PaymentUrl))
            {
                await Launcher.OpenAsync(new Uri(result.PaymentUrl));
                WaitingNotice.IsVisible = true;
                _waitingForSignalR = true;
                App.SignalR.OnPaymentResult += OnPaymentResultReceived;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi kết nối", ex.Message, "OK");
        }
        finally
        {
            if (!_waitingForSignalR)
                ConfirmButton.IsEnabled = true;
        }
    }

    private async void OnPaymentResultReceived(int orderId, bool success, string method, string _ts)
    {
        if (orderId != _orderId) return;

        App.SignalR.OnPaymentResult -= OnPaymentResultReceived;
        _waitingForSignalR = false;

        WaitingNotice.IsVisible = false;
        ConfirmButton.IsEnabled = true;

        if (success)
        {
            await DisplayAlert("Thanh toán thành công", $"Đơn #{orderId} đã được xác nhận.", "OK");
            await NavigateToTracking();
        }
        else
        {
            await DisplayAlert("Thanh toán thất bại", "Vui lòng thử lại hoặc chọn phương thức khác.", "OK");
        }
    }

    private async Task NavigateToTracking()
    {
        await Shell.Current.GoToAsync($"{nameof(OrderTrackingPage)}?orderId={_orderId}");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private sealed record ErrorResp(string message);
}

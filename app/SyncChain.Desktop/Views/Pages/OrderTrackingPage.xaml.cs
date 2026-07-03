using System.Net;
using System.Net.Http.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderTrackingPage : ContentPage
{
    private readonly HttpClient _http;
    private int _orderId;
    private decimal _orderTotal;
    private bool _subscribedToSignalR;

    public int OrderId { get => _orderId; set { _orderId = value; } }

    public OrderTrackingPage() : this(Services.ApiClientProvider.Client) { }

    public OrderTrackingPage(HttpClient http)
    {
        _http = http;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_orderId > 0)
            await LoadAsync();

        App.SignalR.OnOrderStatusUpdated += OnStatusUpdated;
        _subscribedToSignalR = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_subscribedToSignalR)
        {
            App.SignalR.OnOrderStatusUpdated -= OnStatusUpdated;
            _subscribedToSignalR = false;
        }
    }

    private async void OnStatusUpdated(int orderId, string status, string _ts)
    {
        if (orderId != _orderId) return;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var data = await _http.GetFromJsonAsync<OrderTrackingResponse>($"api/order/{_orderId}/tracking");
            if (data?.Order == null)
                return;

            var order = data.Order;
            _orderTotal = order.TongTien;

            // Header + trạng thái (dùng nguồn hiển thị trạng thái dùng chung).
            OrderCodeLabel.Text = $"#ORD-{order.MaDonHang:0000}";
            OrderDateLabel.Text = order.NgayTao.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            (StatusLabel.Text, StatusBadge.BackgroundColor) = OrderStatusDisplay.Badge(order.TrangThai);

            var ptt = string.IsNullOrWhiteSpace(order.PhuongThucThanhToan)
                ? "—"
                : order.PhuongThucThanhToan.ToUpperInvariant();
            PaymentLabel.Text = data.Payment != null
                ? $"{ptt} • {data.Payment.TrangThaiThanhToan}"
                : $"{ptt} • Chưa thanh toán";

            // Chỉ cho thanh toán khi đơn còn chờ duyệt và chưa có giao dịch hoàn tất.
            PayButton.IsVisible = order.TrangThai == "pending" &&
                (data.Payment == null || data.Payment.TrangThaiThanhToan != "Completed");

            RecipientLabel.Text = string.IsNullOrWhiteSpace(order.NguoiNhan) ? "Chưa có thông tin" : order.NguoiNhan;
            AddressLabel.Text = string.IsNullOrWhiteSpace(order.DiaChiGiao) ? "Chưa có địa chỉ" : order.DiaChiGiao;

            // Timeline
            TimelineStack.Children.Clear();
            foreach (var step in data.Timeline)
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = 32 },
                        new ColumnDefinition { Width = GridLength.Star }
                    },
                    ColumnSpacing = 16,
                    Padding       = new Thickness(0, 10)
                };

                var dot = new Border
                {
                    WidthRequest       = 16,
                    HeightRequest      = 16,
                    BackgroundColor    = step.Color,
                    StrokeThickness    = 0,
                    StrokeShape        = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    HorizontalOptions  = LayoutOptions.Center
                };
                row.Add(dot, 0, 0);
                row.Add(new Label
                {
                    Text = step.Label,
                    FontAttributes = step.TrangThai == "hienTai" ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = step.Color,
                    VerticalTextAlignment = TextAlignment.Center
                }, 1, 0);

                TimelineStack.Children.Add(row);
            }

            // Sản phẩm
            TotalLabel.Text = $"{order.TongTien:N0} đ";
            ProductStack.Children.Clear();
            foreach (var item in data.ChiTiet)
            {
                var ten = item.SanPham?.TenSanPham ?? $"SP-{item.MaSanPham}";
                var g = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 16
                };
                g.Add(new Label { Text = ten, VerticalTextAlignment = TextAlignment.Center }, 0, 0);
                g.Add(new Label { Text = $"x{item.SoLuong}", VerticalTextAlignment = TextAlignment.Center,
                    TextColor = Color.FromArgb("#6b7280") }, 1, 0);
                g.Add(new Label { Text = $"{item.DonGia * item.SoLuong:N0} đ",
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.End }, 2, 0);
                ProductStack.Children.Add(g);
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await this.HandleUnauthorizedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] {ex.Message}");
        }
    }

    private async void OnPayClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(PaymentPage)}?orderId={_orderId}&amount={_orderTotal}&orderCode=ORD-{_orderId:0000}");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}

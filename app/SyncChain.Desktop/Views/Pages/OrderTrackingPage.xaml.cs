using System.Net.Http.Json;
using System.Text.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderTrackingPage : ContentPage
{
    private readonly HttpClient _http;
    private int _orderId;
    private bool _subscribedToSignalR;

    public int OrderId { get => _orderId; set { _orderId = value; } }

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
            // Deserialize tracking response
            var json = await _http.GetStringAsync($"api/order/{_orderId}/tracking");
            using var doc  = JsonDocument.Parse(json);
            var root       = doc.RootElement;
            var order      = root.GetProperty("order");
            var timeline   = root.GetProperty("timeline");
            var chiTiet    = root.GetProperty("chiTiet");
            var hasPayment = root.TryGetProperty("payment", out var payment) &&
                             payment.ValueKind != JsonValueKind.Null;

            // Header
            OrderCodeLabel.Text = $"#ORD-{order.GetProperty("maDonHang").GetInt32():0000}";
            OrderDateLabel.Text = order.GetProperty("ngayTao").GetDateTime().ToString("dd/MM/yyyy HH:mm");

            var trangThai = order.GetProperty("trangThai").GetString() ?? "";
            StatusLabel.Text = trangThai switch
            {
                "Draft"      => "Chờ duyệt",
                "Approved"   => "Đã duyệt",
                "Processing" => "Đang xử lý",
                "Done"       => "Hoàn tất",
                "Cancelled"  => "Đã hủy",
                _            => trangThai
            };
            StatusBadge.BackgroundColor = trangThai switch
            {
                "Done"      => Color.FromArgb("#d3e5f1"),
                "Approved"  => Color.FromArgb("#dae2fd"),
                "Processing"=> Color.FromArgb("#dbeafe"),
                "Cancelled" => Color.FromArgb("#ffdad6"),
                _           => Color.FromArgb("#eef0f2")
            };

            var ptt = order.TryGetProperty("phuongThucThanhToan", out var pm)
                ? pm.GetString()?.ToUpperInvariant() ?? "—"
                : "—";
            PaymentLabel.Text = hasPayment
                ? $"{ptt} • {payment.GetProperty("trangThaiThanhToan").GetString()}"
                : $"{ptt} • Chưa thanh toán";

            // Nút Thanh toán chỉ hiển thị khi Draft
            PayButton.IsVisible = trangThai == "Draft";

            // Địa chỉ
            RecipientLabel.Text = order.TryGetProperty("nguoiNhan", out var nr) && nr.ValueKind != JsonValueKind.Null
                ? nr.GetString() : "Chưa có thông tin";
            AddressLabel.Text = order.TryGetProperty("diaChiGiao", out var dc) && dc.ValueKind != JsonValueKind.Null
                ? dc.GetString() : "Chưa có địa chỉ";

            // Timeline
            TimelineStack.Children.Clear();
            foreach (var step in timeline.EnumerateArray())
            {
                var stepName  = step.GetProperty("step").GetString() ?? "";
                var stepState = step.GetProperty("trangThai").GetString() ?? "";
                var color = stepState switch
                {
                    "hoanThanh" => Color.FromArgb("#22c55e"),
                    "hienTai"   => Color.FromArgb("#3b82f6"),
                    "huyBo"     => Color.FromArgb("#ef4444"),
                    _           => Color.FromArgb("#9ca3af")
                };
                var label = stepName switch
                {
                    "Draft"      => "Chờ duyệt",
                    "Approved"   => "Đã duyệt",
                    "Processing" => "Đang xử lý",
                    "Done"       => "Hoàn tất",
                    "Cancelled"  => "Đã hủy",
                    _            => stepName
                };

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
                    BackgroundColor    = color,
                    StrokeThickness    = 0,
                    StrokeShape        = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    HorizontalOptions  = LayoutOptions.Center
                };
                row.Add(dot, 0, 0);
                row.Add(new Label
                {
                    Text = label,
                    FontAttributes = stepState == "hienTai" ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = color,
                    VerticalTextAlignment = TextAlignment.Center
                }, 1, 0);

                TimelineStack.Children.Add(row);
            }

            // Sản phẩm
            var total = order.GetProperty("tongTien").GetDecimal();
            TotalLabel.Text = $"{total:N0} đ";
            ProductStack.Children.Clear();
            foreach (var item in chiTiet.EnumerateArray())
            {
                var sp  = item.GetProperty("sanPham");
                var ten = sp.GetProperty("tenSanPham").GetString() ?? "";
                var qty = item.GetProperty("soLuong").GetInt32();
                var gia = item.GetProperty("donGia").GetDecimal();

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
                g.Add(new Label { Text = $"x{qty}", VerticalTextAlignment = TextAlignment.Center,
                    TextColor = Color.FromArgb("#6b7280") }, 1, 0);
                g.Add(new Label { Text = $"{gia * qty:N0} đ",
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.End }, 2, 0);
                ProductStack.Children.Add(g);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] {ex.Message}");
        }
    }

    private async void OnPayClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(PaymentPage)}?orderId={_orderId}&orderCode=ORD-{_orderId:0000}");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}

using System.Net.Http.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class NotificationPage : ContentPage
{
    private readonly HttpClient _http;
    private List<ThongBaoApi> _items = new();

    public NotificationPage(HttpClient http)
    {
        _http = http;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _items = await _http.GetFromJsonAsync<List<ThongBaoApi>>("api/notification")
                     ?? new();

            var unread = _items.Count(x => !x.DaDoc);
            UnreadBanner.IsVisible = unread > 0;
            UnreadLabel.Text       = $"{unread} thông báo chưa đọc";

            EmptyLabel.IsVisible = _items.Count == 0;
            RenderList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Notif] {ex.Message}");
        }
    }

    private void RenderList()
    {
        NotifStack.Children.Clear();
        foreach (var n in _items)
        {
            var bg = n.DaDoc ? Color.FromArgb("#f9fafb") : Color.FromArgb("#eff6ff");
            var icon = n.LoaiThongBao == "payment_result" ? "💳" : "📦";

            var border = new Border
            {
                BackgroundColor = bg,
                StrokeThickness = 0,
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Padding         = new Thickness(16)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 14
            };

            // Icon badge
            var iconBorder = new Border
            {
                WidthRequest    = 38,
                HeightRequest   = 38,
                BackgroundColor = Color.FromArgb("#e0e7ff"),
                StrokeThickness = 0,
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                VerticalOptions = LayoutOptions.Center
            };
            iconBorder.Content = new Label
            {
                Text = icon,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center,
                FontSize = 16
            };
            grid.Add(iconBorder, 0, 0);

            var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            textStack.Add(new Label
            {
                Text           = n.TieuDe,
                FontAttributes = n.DaDoc ? FontAttributes.None : FontAttributes.Bold,
                FontSize       = 14
            });
            textStack.Add(new Label
            {
                Text      = n.NoiDung,
                FontSize  = 13,
                TextColor = Color.FromArgb("#6b7280"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines      = 2
            });
            textStack.Add(new Label
            {
                Text      = n.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                FontSize  = 11,
                TextColor = Color.FromArgb("#9ca3af")
            });
            grid.Add(textStack, 1, 0);

            border.Content = grid;

            // Tap → mark read + navigate
            var captured = n;
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (_, _) => await OnItemTapped(captured);
            border.GestureRecognizers.Add(tapGesture);

            NotifStack.Children.Add(border);
        }
    }

    private async Task OnItemTapped(ThongBaoApi notif)
    {
        if (!notif.DaDoc)
        {
            try
            {
                await _http.PutAsync($"api/notification/{notif.MaThongBao}/read", null);
                notif.DaDoc = true;
                await LoadAsync();
            }
            catch { }
        }

        if (notif.MaDonHang.HasValue)
            await Shell.Current.GoToAsync(
                $"{nameof(OrderTrackingPage)}?orderId={notif.MaDonHang.Value}");
    }

    private async void OnMarkAllReadClicked(object? sender, EventArgs e)
    {
        try
        {
            await _http.PutAsync("api/notification/read-all", null);
            await LoadAsync();
        }
        catch { }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
        => await LoadAsync();
}

using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class ImportsPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<ImportItem> Imports { get; private set; } = Array.Empty<ImportItem>();
	public IReadOnlyList<SupplierItem> Suppliers { get; private set; } = Array.Empty<SupplierItem>();

	public ImportsPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadImportsAsync();
	}

	private async Task LoadImportsAsync()
	{
		try
		{
			var response = await _http.GetFromJsonAsync<ApiResponse<List<SanPhamApi>>>("api/sanpham");
			if (response?.success == true && response.data != null)
			{
				var products = response.data;

				// Tạo ImportItem từ sản phẩm (giả lập đơn nhập dựa trên tồn kho)
				Imports = products.Select(p => new ImportItem
				{
					Code = $"NH-{p.MaSanPham:0000}",
					Supplier = "Nhà cung cấp ABC",
					Date = DateTime.Now.AddDays(-Random.Shared.Next(1, 30)).ToString("dd/MM/yyyy"),
					ProductCount = p.TenSanPham,
					Amount = $"{p.GiaBan * p.SoLuongTon:N0} đ",
					Status = p.SoLuongTon > p.MucTonThap ? "Đã nhập" :
							 p.SoLuongTon > 0 ? "Đang vận chuyển" : "Chờ duyệt",
					StatusColor = p.SoLuongTon > p.MucTonThap ? Color.FromArgb("#d3e5f1") :
								  Color.FromArgb("#dae2fd")
				}).ToList();

				// Tạo SupplierItem
				Suppliers = new List<SupplierItem>
				{
					new() { Name = "Nhà cung cấp ABC", Orders = products.Count.ToString(),
						Amount = $"{products.Sum(p => p.GiaBan * p.SoLuongTon):N0} đ",
						Initial = "A", Accent = Color.FromArgb("#213145") },
					new() { Name = "Nhà cung cấp XYZ", Orders = (products.Count / 2).ToString(),
						Amount = $"{products.Sum(p => p.GiaBan * p.SoLuongTon) / 2:N0} đ",
						Initial = "X", Accent = Color.FromArgb("#50616B") }
				};

				OnPropertyChanged(nameof(Imports));
				OnPropertyChanged(nameof(Suppliers));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[ImportsPage] Load error: {ex.Message}");
		}
	}
}

using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class ImportsPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<ImportItem> Imports { get; private set; } = Array.Empty<ImportItem>();

	// Thống kê
	public string TotalImports { get; private set; } = "0";
	public string TotalQuantity { get; private set; } = "0";
	public string TotalAmount { get; private set; } = "0 đ";
	public string PaginationText { get; private set; } = "Đang tải...";

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
			// Gọi API lấy lịch sử nhập kho
			var imports = await _http.GetFromJsonAsync<List<ImportApi>>("api/product/imports");
			if (imports != null)
			{
				Imports = imports.Select(MapToImportItem).ToList();

				TotalImports = imports.Count.ToString();
				TotalQuantity = imports.Sum(i => i.SoLuong).ToString();
				TotalAmount = $"{imports.Sum(i => i.ThanhTien):N0} đ";
				PaginationText = $"Hiển thị 1 - {imports.Count} trong số {imports.Count} giao dịch";

				OnPropertyChanged(nameof(Imports));
				OnPropertyChanged(nameof(TotalImports));
				OnPropertyChanged(nameof(TotalQuantity));
				OnPropertyChanged(nameof(TotalAmount));
				OnPropertyChanged(nameof(PaginationText));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[ImportsPage] Load error: {ex.Message}");
		}
	}

	private static ImportItem MapToImportItem(ImportApi imp)
	{
		return new ImportItem
		{
			Code = $"NH-{imp.MaGiaoDich:0000}",
			Supplier = imp.TenSanPham,
			Date = imp.ThoiGian.ToString("dd/MM/yyyy HH:mm"),
			ProductCount = $"+{imp.SoLuong} sản phẩm",
			Amount = $"{imp.ThanhTien:N0} đ",
			Status = "Đã nhập",
			StatusColor = Color.FromArgb("#d3e5f1")
		};
	}
}

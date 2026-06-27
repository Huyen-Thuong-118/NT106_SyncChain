using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Text.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class ImportsPage : ContentPage
{
	private readonly HttpClient _http;
	private List<WarehouseReceiptApi> _receipts = [];
	private List<SanPhamApi> _products = [];
	private WarehouseReceiptApi? _selected;
	private bool _isReceiptFormOpen;

	public IReadOnlyList<ImportItem> Imports { get; private set; } = [];
	public IReadOnlyList<TimelineItem> ReceiptTimeline { get; private set; } = [];
	public string TotalImports { get; private set; } = "0";
	public string TotalQuantity { get; private set; } = "0";
	public string TotalAmount { get; private set; } = "0 đ";
	public string PendingCount { get; private set; } = "0";
	public string PaginationText { get; private set; } = string.Empty;
	public string SelectedReceiptCode { get; private set; } = "Chọn một phiếu để xem";
	public string SelectedStatusText { get; private set; } = "Chưa chọn";
	public Color SelectedStatusColor { get; private set; } = Colors.LightGray;
	public string SelectedSupplier { get; private set; } = "—";
	public string SelectedContact { get; private set; } = "—";
	public string SelectedProductCount { get; private set; } = "0";
	public string SelectedQuantity { get; private set; } = "0";
	public string SelectedAmount { get; private set; } = "0 đ";
	public string SelectedNote { get; private set; } = string.Empty;
	public string PrimaryActionText { get; private set; } = string.Empty;
	public bool HasPrimaryAction { get; private set; }
	public bool CanCancelSelected { get; private set; }
	public ObservableCollection<ReceiptProductOption> ReceiptProductOptions { get; } = [];
	public ObservableCollection<ReceiptDraftLine> ReceiptDraftLines { get; } = [];
	public bool IsReceiptFormOpen
	{
		get => _isReceiptFormOpen;
		private set
		{
			if (_isReceiptFormOpen == value) return;
			_isReceiptFormOpen = value;
			OnPropertyChanged();
		}
	}
	public bool IsReceiptDraftEmpty => ReceiptDraftLines.Count == 0;
	public string ReceiptDraftTotalText => $"{ReceiptDraftLines.Sum(x => x.LineTotal):N0} đ";

	public ImportsPage() : this(Services.ApiClientProvider.Client) { }

	public ImportsPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadAsync();
	}

	private async Task LoadAsync(int? selectId = null)
	{
		try
		{
			var receiptsTask = _http.GetFromJsonAsync<List<WarehouseReceiptApi>>("api/warehouse-receipts");
			var productsTask = _http.GetFromJsonAsync<List<SanPhamApi>>("api/product");
			await Task.WhenAll(receiptsTask, productsTask);
			_receipts = receiptsTask.Result ?? [];
			_products = productsTask.Result ?? [];
			RefreshReceiptProductOptions();

			Imports = _receipts.Select(MapReceipt).ToList();
			TotalImports = _receipts.Count.ToString("N0");
			TotalQuantity = _receipts
				.Where(x => x.TrangThai == "completed")
				.Sum(x => x.ChiTiet.Sum(i => i.SoLuong)).ToString("N0");
			TotalAmount = $"{_receipts.Where(x => x.TrangThai == "completed").Sum(x => x.TongTien):N0} đ";
			PendingCount = _receipts.Count(x => x.TrangThai is "draft" or "pending" or "approved").ToString("N0");
			PaginationText = $"Hiển thị {_receipts.Count:N0} phiếu nhập";

			var selected = selectId.HasValue
				? _receipts.FirstOrDefault(x => x.MaPhieuNhap == selectId.Value)
				: _selected == null ? _receipts.FirstOrDefault() :
					_receipts.FirstOrDefault(x => x.MaPhieuNhap == _selected.MaPhieuNhap);
			if (selected != null) SelectReceipt(selected);
			NotifyAll();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không tải được phiếu nhập", ex.Message, "OK");
		}
	}

	private async void OnCreateReceiptClicked(object? sender, EventArgs e)
	{
		if (_products.Count == 0)
		{
			await DisplayAlertAsync("Không có sản phẩm", "Cần tạo sản phẩm trước khi nhập kho.", "OK");
			return;
		}
		ResetReceiptForm();
		IsReceiptFormOpen = true;
	}

	private void OnAddReceiptLineClicked(object? sender, EventArgs e)
	{
		if (ReceiptProductPicker.SelectedItem is not ReceiptProductOption product)
			return;
		if (!int.TryParse(ReceiptQuantityEntry.Text, out var quantity) || quantity <= 0)
			return;
		if (!decimal.TryParse(ReceiptUnitCostEntry.Text, out var cost) || cost < 0)
			return;

		ReceiptDraftLines.Add(new ReceiptDraftLine
		{
			ProductId = product.ProductId,
			ProductName = product.Name,
			Quantity = quantity,
			UnitCost = cost
		});
		ReceiptProductPicker.SelectedItem = null;
		ReceiptQuantityEntry.Text = "1";
		ReceiptUnitCostEntry.Text = string.Empty;
		RefreshReceiptProductOptions();
		NotifyReceiptDraft();
	}

	private void OnRemoveReceiptLineClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button ||
			!int.TryParse(button.CommandParameter?.ToString(), out var productId))
			return;
		var line = ReceiptDraftLines.FirstOrDefault(x => x.ProductId == productId);
		if (line != null) ReceiptDraftLines.Remove(line);
		RefreshReceiptProductOptions();
		NotifyReceiptDraft();
	}

	private void OnReceiptLineChanged(object? sender, FocusEventArgs e)
	{
		RefreshReceiptDraftLines();
	}

	private async void OnSubmitReceiptFormClicked(object? sender, EventArgs e)
	{
		var supplier = ReceiptSupplierEntry.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(supplier))
		{
			await DisplayAlertAsync("Thiếu nhà cung cấp", "Vui lòng nhập nhà cung cấp hoặc nguồn nhập.", "OK");
			return;
		}
		if (ReceiptDraftLines.Count == 0 ||
			ReceiptDraftLines.Any(x => x.Quantity <= 0 || x.UnitCost < 0))
		{
			await DisplayAlertAsync("Hàng hóa không hợp lệ", "Phiếu phải có sản phẩm, số lượng và đơn giá hợp lệ.", "OK");
			return;
		}

		var response = await _http.PostAsJsonAsync("api/warehouse-receipts", new
		{
			tenNguonNhap = supplier,
			diaChiNguonNhap = ReceiptAddressEntry.Text?.Trim() ?? string.Empty,
			nguoiLienHe = ReceiptContactEntry.Text?.Trim() ?? string.Empty,
			ghiChu = ReceiptNoteEntry.Text?.Trim() ?? string.Empty,
			chiTiet = ReceiptDraftLines.Select(x => new
			{
				maSanPham = x.ProductId,
				soLuong = x.Quantity,
				donGiaNhap = x.UnitCost
			}).ToList()
		});
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không tạo được phiếu", await ReadErrorAsync(response), "OK");
			return;
		}
		var created = await response.Content.ReadFromJsonAsync<WarehouseReceiptApi>();
		IsReceiptFormOpen = false;
		await LoadAsync(created?.MaPhieuNhap);
	}

	private void OnCloseReceiptFormClicked(object? sender, EventArgs e)
	{
		IsReceiptFormOpen = false;
	}

	private void ResetReceiptForm()
	{
		ReceiptSupplierEntry.Text = string.Empty;
		ReceiptAddressEntry.Text = string.Empty;
		ReceiptContactEntry.Text = string.Empty;
		ReceiptNoteEntry.Text = string.Empty;
		ReceiptProductPicker.SelectedItem = null;
		ReceiptQuantityEntry.Text = "1";
		ReceiptUnitCostEntry.Text = string.Empty;
		ReceiptDraftLines.Clear();
		RefreshReceiptProductOptions();
		NotifyReceiptDraft();
	}

	private void RefreshReceiptProductOptions()
	{
		ReceiptProductOptions.Clear();
		foreach (var product in _products
			.Where(x => ReceiptDraftLines.All(line => line.ProductId != x.MaSanPham))
			.OrderBy(x => x.TenSanPham))
		{
			ReceiptProductOptions.Add(new ReceiptProductOption
			{
				ProductId = product.MaSanPham,
				Name = product.TenSanPham,
				CurrentCost = product.GiaNhap
			});
		}
		OnPropertyChanged(nameof(ReceiptProductOptions));
	}

	private void RefreshReceiptDraftLines()
	{
		var lines = ReceiptDraftLines.ToList();
		ReceiptDraftLines.Clear();
		foreach (var line in lines) ReceiptDraftLines.Add(line);
		NotifyReceiptDraft();
	}

	private void NotifyReceiptDraft()
	{
		OnPropertyChanged(nameof(ReceiptDraftLines));
		OnPropertyChanged(nameof(IsReceiptDraftEmpty));
		OnPropertyChanged(nameof(ReceiptDraftTotalText));
	}

	private void OnSelectReceiptClicked(object? sender, EventArgs e)
	{
		if (sender is Button button && int.TryParse(button.CommandParameter?.ToString(), out var id))
		{
			var receipt = _receipts.FirstOrDefault(x => x.MaPhieuNhap == id);
			if (receipt != null) SelectReceipt(receipt);
		}
	}

	private async void OnPrimaryActionClicked(object? sender, EventArgs e)
	{
		if (_selected == null) return;
		var action = _selected.TrangThai switch
		{
			"draft" => "submit",
			"pending" => "approve",
			"approved" => "complete",
			_ => string.Empty
		};
		if (action == string.Empty) return;
		var response = await _http.PutAsync($"api/warehouse-receipts/{_selected.MaPhieuNhap}/{action}", null);
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không cập nhật được phiếu", await ReadErrorAsync(response), "OK");
			return;
		}
		await LoadAsync(_selected.MaPhieuNhap);
	}

	private async void OnCancelReceiptClicked(object? sender, EventArgs e)
	{
		if (_selected == null) return;
		if (!await DisplayAlertAsync("Hủy phiếu nhập", "Bạn có chắc muốn hủy phiếu này?", "Hủy phiếu", "Không"))
			return;
		var response = await _http.PutAsync($"api/warehouse-receipts/{_selected.MaPhieuNhap}/cancel", null);
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không hủy được phiếu", await ReadErrorAsync(response), "OK");
			return;
		}
		await LoadAsync(_selected.MaPhieuNhap);
	}

	private async void OnReloadClicked(object? sender, EventArgs e) => await LoadAsync();

	private void SelectReceipt(WarehouseReceiptApi receipt)
	{
		_selected = receipt;
		var display = StatusDisplay(receipt.TrangThai);
		SelectedReceiptCode = receipt.SoPhieu;
		SelectedStatusText = display.Text;
		SelectedStatusColor = display.Color;
		SelectedSupplier = receipt.TenNguonNhap;
		SelectedContact = string.IsNullOrWhiteSpace(receipt.NguoiLienHe) ? "Chưa cập nhật" : receipt.NguoiLienHe;
		SelectedProductCount = $"{receipt.ChiTiet.Count:N0} mặt hàng";
		SelectedQuantity = $"{receipt.ChiTiet.Sum(x => x.SoLuong):N0} sản phẩm";
		SelectedAmount = $"{receipt.TongTien:N0} đ";
		SelectedNote = string.IsNullOrWhiteSpace(receipt.GhiChu) ? "Không có ghi chú." : receipt.GhiChu;
		(PrimaryActionText, HasPrimaryAction) = receipt.TrangThai switch
		{
			"draft" => ("GỬI DUYỆT", true),
			"pending" => ("DUYỆT PHIẾU", true),
			"approved" => ("HOÀN TẤT NHẬP KHO", true),
			_ => (string.Empty, false)
		};
		CanCancelSelected = receipt.TrangThai is "pending" or "approved";
		ReceiptTimeline = BuildTimeline(receipt);
		NotifySelected();
	}

	private static IReadOnlyList<TimelineItem> BuildTimeline(WarehouseReceiptApi receipt)
	{
		var order = StatusOrder(receipt.TrangThai);
		var items = new List<TimelineItem>
		{
			Step("Phiếu nhập đã được tạo", receipt.NgayTao.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), true, order == 0),
			Step("Chờ quản lý duyệt", receipt.TrangThai == "pending" ? "Đang chờ xử lý" :
				receipt.NgayDuyet?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Chờ xử lý", order >= 1, order == 1),
			Step("Phiếu nhập đã được duyệt", receipt.NgayDuyet?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Chờ xử lý", order >= 2, order == 2),
			Step("Hàng đã nhập vào kho", receipt.NgayHoanTat?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Chờ xử lý", order >= 3, order == 3)
		};
		if (receipt.TrangThai == "cancelled")
			items.Add(new TimelineItem { Title = "Phiếu nhập đã hủy", Time = "Kết thúc", Accent = Colors.Red });
		items[^1].ShowConnector = false;
		return items;
	}

	private static TimelineItem Step(string title, string time, bool completed, bool current) => new()
	{
		Title = title,
		Time = time,
		State = current ? "current" : completed ? "completed" : "pending",
		Accent = current ? Colors.Blue : completed ? Colors.Green : Colors.Gray
	};

	private static int StatusOrder(string status) => status switch
	{
		"draft" => 0, "pending" => 1, "approved" => 2, "completed" => 3, _ => -1
	};

	private static ImportItem MapReceipt(WarehouseReceiptApi receipt)
	{
		var status = StatusDisplay(receipt.TrangThai);
		return new ImportItem
		{
			Id = receipt.MaPhieuNhap,
			Code = receipt.SoPhieu,
			Supplier = receipt.TenNguonNhap,
			Date = receipt.NgayTao.ToLocalTime().ToString("dd/MM/yyyy"),
			ProductCount = $"{receipt.ChiTiet.Count:N0} mặt hàng",
			Amount = $"{receipt.TongTien:N0} đ",
			Status = status.Text,
			StatusColor = status.Color
		};
	}

	private static (string Text, Color Color) StatusDisplay(string status) => status switch
	{
		"draft" => ("Nháp", Color.FromArgb("#eef0f2")),
		"pending" => ("Chờ duyệt", Color.FromArgb("#dae2fd")),
		"approved" => ("Đã duyệt", Color.FromArgb("#dbeafe")),
		"completed" => ("Đã nhập", Color.FromArgb("#d3e5f1")),
		"cancelled" => ("Đã hủy", Color.FromArgb("#ffdad6")),
		_ => (status, Colors.LightGray)
	};

	private void NotifyAll()
	{
		foreach (var name in new[] { nameof(Imports), nameof(TotalImports), nameof(TotalQuantity),
			nameof(TotalAmount), nameof(PendingCount), nameof(PaginationText) }) OnPropertyChanged(name);
	}

	private void NotifySelected()
	{
		foreach (var name in new[] { nameof(ReceiptTimeline), nameof(SelectedReceiptCode),
			nameof(SelectedStatusText), nameof(SelectedStatusColor), nameof(SelectedSupplier),
			nameof(SelectedContact), nameof(SelectedProductCount), nameof(SelectedQuantity),
			nameof(SelectedAmount), nameof(SelectedNote), nameof(PrimaryActionText),
			nameof(HasPrimaryAction), nameof(CanCancelSelected) }) OnPropertyChanged(name);
	}

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
	{
		var text = await response.Content.ReadAsStringAsync();
		try
		{
			using var json = JsonDocument.Parse(text);
			if (json.RootElement.TryGetProperty("message", out var message))
				return message.GetString() ?? text;
		}
		catch { }
		return text.Trim('"');
	}
}

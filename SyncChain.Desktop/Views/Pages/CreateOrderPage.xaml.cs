using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CreateOrderPage : ContentPage
{
	public ObservableCollection<ProductItem> Products { get; } = new();
	public ObservableCollection<CreateOrderLine> Lines { get; } = new();
	public IReadOnlyList<PaymentOption> Payments => DemoData.Payments;

	public CreateOrderPage()
	{
		InitializeComponent();
		BindingContext = this;
		UpdateTotals();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProductsAsync();
	}

	// Tải các sản phẩm còn tồn kho để đưa vào đơn hàng.
	private async Task LoadProductsAsync()
	{
		try
		{
			var products = await SyncChainApiClient.Instance.GetProductsAsync();
			Products.Clear();

			foreach (var product in products.Where(x => x.StockQuantity > 0))
			{
				Products.Add(product);
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không tải được sản phẩm", ex.Message, "OK");
		}
	}

	// Thêm sản phẩm vào giỏ tạm và kiểm tra số lượng tồn.
	private async void OnAddProductClicked(object? sender, EventArgs e)
	{
		if (ProductPicker.SelectedItem is not ProductItem product)
		{
			await DisplayAlert("Tạo đơn hàng", "Vui lòng chọn sản phẩm.", "OK");
			return;
		}

		if (!int.TryParse(QuantityEntry.Text, out var quantity) || quantity <= 0)
		{
			await DisplayAlert("Tạo đơn hàng", "Số lượng phải lớn hơn 0.", "OK");
			return;
		}

		var existingLine = Lines.FirstOrDefault(x => x.ProductId == product.Id);
		var currentQuantity = existingLine?.Quantity ?? 0;

		if (currentQuantity + quantity > product.StockQuantity)
		{
			await DisplayAlert("Tạo đơn hàng", $"Sản phẩm chỉ còn {product.StockQuantity} trong kho.", "OK");
			return;
		}

		if (existingLine == null)
		{
			Lines.Add(new CreateOrderLine(product, quantity));
		}
		else
		{
			existingLine.Quantity += quantity;
		}

		QuantityEntry.Text = "1";
		UpdateTotals();
	}

	// Giảm số lượng dòng hàng hoặc xóa dòng nếu còn 1.
	private void OnDecreaseQuantityClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not CreateOrderLine line)
			return;

		if (line.Quantity <= 1)
		{
			Lines.Remove(line);
		}
		else
		{
			line.Quantity--;
		}

		UpdateTotals();
	}

	// Tăng số lượng dòng hàng nhưng không vượt tồn kho.
	private async void OnIncreaseQuantityClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not CreateOrderLine line)
			return;

		if (line.Quantity >= line.StockQuantity)
		{
			await DisplayAlert("Tạo đơn hàng", $"Sản phẩm chỉ còn {line.StockQuantity} trong kho.", "OK");
			return;
		}

		line.Quantity++;
		UpdateTotals();
	}

	// Xóa một dòng sản phẩm khỏi đơn tạm.
	private void OnRemoveLineClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is CreateOrderLine line)
		{
			Lines.Remove(line);
			UpdateTotals();
		}
	}

	// Làm sạch form tạo đơn hàng.
	private void OnResetClicked(object? sender, EventArgs e)
	{
		ProductPicker.SelectedItem = null;
		QuantityEntry.Text = "1";
		Lines.Clear();
		UpdateTotals();
	}

	// Gửi đơn hàng mới lên API và quay về danh sách đơn.
	private async void OnCreateOrderClicked(object? sender, EventArgs e)
	{
		if (Lines.Count == 0)
		{
			await DisplayAlert("Tạo đơn hàng", "Vui lòng thêm ít nhất một sản phẩm.", "OK");
			return;
		}

		CreateOrderButton.IsEnabled = false;

		try
		{
			var result = await SyncChainApiClient.Instance.CreateOrderAsync(Lines.Select(x => new SyncChainApiClient.CreateOrderLineRequest
			{
				MaSanPham = x.ProductId,
				SoLuong = x.Quantity
			}));

			await DisplayAlert("Tạo đơn hàng", $"Tạo đơn #{result.MaDonHang} thành công. Tổng tiền: {FormatMoney(result.TongTien)}", "OK");
			Lines.Clear();
			UpdateTotals();
			await Shell.Current.GoToAsync("//orders");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không tạo được đơn hàng", ex.Message, "OK");
		}
		finally
		{
			CreateOrderButton.IsEnabled = true;
		}
	}

	// Quay lại danh sách đơn hàng.
	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//orders");
	}

	// Tính lại tạm tính, tổng tiền và trạng thái nút tạo đơn.
	private void UpdateTotals()
	{
		var subtotal = Lines.Sum(x => x.LineTotal);
		SubtotalLabel.Text = FormatMoney(subtotal);
		TotalLabel.Text = FormatMoney(subtotal);
		CreateOrderButton.IsEnabled = Lines.Count > 0;
	}

	// Định dạng tiền hiển thị theo VND.
	private static string FormatMoney(decimal value)
	{
		return value.ToString("N0", CultureInfo.InvariantCulture) + " VND";
	}
}

public sealed class CreateOrderLine : INotifyPropertyChanged
{
	private int _quantity;

	// Lưu thông tin một dòng sản phẩm trong đơn tạm.
	public CreateOrderLine(ProductItem product, int quantity)
	{
		ProductId = product.Id;
		Name = product.Name;
		Initials = product.Initials;
		UnitPrice = product.UnitPrice;
		StockQuantity = product.StockQuantity;
		_quantity = quantity;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public int ProductId { get; }
	public string Name { get; }
	public string Initials { get; }
	public decimal UnitPrice { get; }
	public int StockQuantity { get; }
	public string Variant => $"Tồn kho: {StockQuantity}";
	public string Price => UnitPrice.ToString("N0", CultureInfo.InvariantCulture) + " VND";
	public decimal LineTotal => UnitPrice * Quantity;
	public string LineTotalText => LineTotal.ToString("N0", CultureInfo.InvariantCulture) + " VND";

	public int Quantity
	{
		get => _quantity;
		set
		{
			if (_quantity == value)
				return;

			_quantity = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(LineTotal));
			OnPropertyChanged(nameof(LineTotalText));
		}
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		// Báo cho UI cập nhật lại binding khi dữ liệu dòng hàng đổi.
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

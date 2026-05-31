using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class UserAccessPage : ContentPage
{
	private readonly HttpClient _http;

	// Thống kê
	public string TotalUsers { get; private set; } = "0";
	public string AdminCount { get; private set; } = "0";
	public string ManagerCount { get; private set; } = "0";
	public string StaffCount { get; private set; } = "0";

	// Danh sách người dùng
	public IReadOnlyList<UserApi> Users { get; private set; } = Array.Empty<UserApi>();

	public IReadOnlyList<RoleOption> Roles { get; } =
	[
		new() { Name = "Admin", Description = "Quản trị hệ thống, toàn quyền truy cập", IsSelected = true },
		new() { Name = "Manager", Description = "Quản lý kho hàng và đơn hàng", IsSelected = false },
		new() { Name = "Staff", Description = "Nhân viên xử lý đơn và kho", IsSelected = false }
	];

	public UserAccessPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadUsersAsync();
	}

	private async Task LoadUsersAsync()
	{
		try
		{
			// Gọi API lấy danh sách người dùng
			var users = await _http.GetFromJsonAsync<List<UserApi>>("api/admin/users");
			if (users != null)
			{
				Users = users;

				// Thống kê
				TotalUsers = users.Count.ToString();
				AdminCount = users.Count(u => u.Role == "admin").ToString();
				ManagerCount = users.Count(u => u.Role == "manager").ToString();
				StaffCount = users.Count(u => u.Role == "staff").ToString();

				OnPropertyChanged(nameof(Users));
				OnPropertyChanged(nameof(TotalUsers));
				OnPropertyChanged(nameof(AdminCount));
				OnPropertyChanged(nameof(ManagerCount));
				OnPropertyChanged(nameof(StaffCount));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[UserAccessPage] Load error: {ex.Message}");
		}
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		App.ShowLogin();
	}
}

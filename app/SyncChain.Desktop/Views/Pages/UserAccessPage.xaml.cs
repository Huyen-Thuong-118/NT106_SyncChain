using System.Net.Http.Json;
using System.Text.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class UserAccessPage : ContentPage
{
	private readonly HttpClient _http;
	private bool _isBusy;
	private bool _isLoginHistoryOpen;

	public string TotalUsers { get; private set; } = "0";
	public string AdminCount { get; private set; } = "0";
	public string ManagerCount { get; private set; } = "0";
	public string StaffCount { get; private set; } = "0";
	public IReadOnlyList<UserApi> Users { get; private set; } = Array.Empty<UserApi>();
	public IReadOnlyList<LoginHistoryApi> LoginHistory { get; private set; } = Array.Empty<LoginHistoryApi>();
	public string LoginHistoryUserText { get; private set; } = string.Empty;
	public bool IsLoginHistoryOpen
	{
		get => _isLoginHistoryOpen;
		private set
		{
			if (_isLoginHistoryOpen == value) return;
			_isLoginHistoryOpen = value;
			OnPropertyChanged();
		}
	}

	public UserAccessPage() : this(Services.ApiClientProvider.Client) { }

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
		if (_isBusy) return;
		_isBusy = true;
		try
		{
			var users = await _http.GetFromJsonAsync<List<UserApi>>("api/admin/users") ?? [];
			Users = users
				.OrderBy(x => RoleOrder(x.Role))
				.ThenBy(x => x.Email)
				.ToList();
			TotalUsers = Users.Count(x => x.IsActive).ToString("N0");
			AdminCount = Users.Count(x => x.Role == "admin").ToString("N0");
			ManagerCount = Users.Count(x => x.Role == "manager").ToString("N0");
			StaffCount = Users.Count(x => x.Role == "staff").ToString("N0");
			NotifyAll();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không tải được người dùng", ex.Message, "OK");
		}
		finally
		{
			_isBusy = false;
		}
	}

	private async void OnCreateUserClicked(object? sender, EventArgs e)
	{
		var username = NewUsernameEntry.Text?.Trim() ?? string.Empty;
		var email = NewEmailEntry.Text?.Trim() ?? string.Empty;
		var password = NewPasswordEntry.Text ?? string.Empty;
		var role = NewRolePicker.SelectedItem?.ToString()?.ToLowerInvariant() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(username))
		{
			await DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập họ tên hoặc tên đăng nhập.", "OK");
			return;
		}
		if (!System.Net.Mail.MailAddress.TryCreate(email, out _))
		{
			await DisplayAlertAsync("Email không hợp lệ", "Vui lòng kiểm tra email công việc.", "OK");
			return;
		}
		if (password.Length < 6)
		{
			await DisplayAlertAsync("Mật khẩu không hợp lệ", "Mật khẩu phải có ít nhất 6 ký tự.", "OK");
			return;
		}

		await RunMutationAsync(
			() => _http.PostAsJsonAsync("api/admin/users", new { username, email, password, role }),
			"Đã tạo tài khoản.",
			resetForm: true);
	}

	private async void OnUserActionsClicked(object? sender, EventArgs e)
	{
		var user = FindUser(sender);
		if (user == null) return;

		var activeAction = user.IsActive ? "Khóa tài khoản" : "Mở khóa tài khoản";
		var selected = await DisplayActionSheetAsync(
			$"Thao tác · {user.TenDangNhap}",
			"Đóng",
			null,
			"Đổi vai trò",
			activeAction,
			"Reset mật khẩu",
			"Lịch sử đăng nhập");

		switch (selected)
		{
			case "Đổi vai trò":
				OnChangeRoleClicked(sender, e);
				break;
			case "Khóa tài khoản":
			case "Mở khóa tài khoản":
				OnToggleActiveClicked(sender, e);
				break;
			case "Reset mật khẩu":
				OnResetPasswordClicked(sender, e);
				break;
			case "Lịch sử đăng nhập":
				OnViewLoginHistoryClicked(sender, e);
				break;
		}
	}

	private async void OnChangeRoleClicked(object? sender, EventArgs e)
	{
		var user = FindUser(sender);
		if (user == null) return;

		var selected = await DisplayActionSheetAsync(
			$"Đổi vai trò: {user.TenDangNhap}",
			"Hủy", null, "Admin", "Manager", "Staff");
		if (selected is not ("Admin" or "Manager" or "Staff")) return;

		var role = selected.ToLowerInvariant();
		if (role == user.Role) return;

		await RunMutationAsync(
			() => _http.PutAsJsonAsync($"api/admin/users/{user.MaNguoiDung}", new
			{
				username = user.TenDangNhap,
				email = user.Email,
				role,
				isActive = user.IsActive
			}),
			$"Đã chuyển tài khoản sang {selected}.");
	}

	private async void OnToggleActiveClicked(object? sender, EventArgs e)
	{
		var user = FindUser(sender);
		if (user == null) return;

		var action = user.IsActive ? "khóa" : "mở khóa";
		if (!await DisplayAlertAsync(
			$"{char.ToUpperInvariant(action[0])}{action[1..]} tài khoản",
			$"Bạn có chắc muốn {action} tài khoản {user.Email}?",
			"Đồng ý", "Hủy"))
			return;

		await RunMutationAsync(
			() => _http.PutAsJsonAsync(
				$"api/admin/users/{user.MaNguoiDung}/active",
				new { isActive = !user.IsActive }),
			$"Đã {action} tài khoản.");
	}

	private async void OnResetPasswordClicked(object? sender, EventArgs e)
	{
		var user = FindUser(sender);
		if (user == null) return;

		var password = await DisplayPromptAsync(
			"Reset mật khẩu",
			$"Nhập mật khẩu mới cho {user.Email}:",
			"Xác nhận", "Hủy",
			placeholder: "Tối thiểu 6 ký tự",
			maxLength: 100,
			keyboard: Keyboard.Text);
		if (password == null) return;
		if (password.Length < 6)
		{
			await DisplayAlertAsync("Mật khẩu không hợp lệ", "Mật khẩu phải có ít nhất 6 ký tự.", "OK");
			return;
		}

		await RunMutationAsync(
			() => _http.PutAsJsonAsync(
				$"api/admin/users/{user.MaNguoiDung}/password",
				new { password }),
			"Đã reset mật khẩu.");
	}

	private async void OnViewLoginHistoryClicked(object? sender, EventArgs e)
	{
		var user = FindUser(sender);
		if (user == null) return;

		try
		{
			LoginHistory = await _http.GetFromJsonAsync<List<LoginHistoryApi>>(
				$"api/admin/users/{user.MaNguoiDung}/login-history") ?? [];
			LoginHistoryUserText = $"{user.TenDangNhap} · {user.Email}";
			OnPropertyChanged(nameof(LoginHistory));
			OnPropertyChanged(nameof(LoginHistoryUserText));
			IsLoginHistoryOpen = true;
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không tải được lịch sử đăng nhập", ex.Message, "OK");
		}
	}

	private void OnCloseLoginHistoryClicked(object? sender, EventArgs e)
	{
		IsLoginHistoryOpen = false;
	}

	private async Task RunMutationAsync(
		Func<Task<HttpResponseMessage>> request,
		string successMessage,
		bool resetForm = false)
	{
		if (_isBusy) return;
		_isBusy = true;
		try
		{
			var response = await request();
			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException(await ReadErrorAsync(response));

			if (resetForm) ResetForm();
			await DisplayAlertAsync("Thành công", successMessage, "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không thực hiện được", ex.Message, "OK");
		}
		finally
		{
			_isBusy = false;
		}
		await LoadUsersAsync();
	}

	private UserApi? FindUser(object? sender)
	{
		if (sender is not Button button ||
			!int.TryParse(button.CommandParameter?.ToString(), out var id))
			return null;
		return Users.FirstOrDefault(x => x.MaNguoiDung == id);
	}

	private void OnResetFormClicked(object? sender, EventArgs e) => ResetForm();
	private async void OnReloadClicked(object? sender, EventArgs e) => await LoadUsersAsync();

	private void ResetForm()
	{
		NewUsernameEntry.Text = string.Empty;
		NewEmailEntry.Text = string.Empty;
		NewPasswordEntry.Text = string.Empty;
		NewRolePicker.SelectedIndex = 2;
	}

	private void NotifyAll()
	{
		foreach (var name in new[]
		{
			nameof(Users), nameof(TotalUsers), nameof(AdminCount),
			nameof(ManagerCount), nameof(StaffCount)
		})
			OnPropertyChanged(name);
	}

	private static int RoleOrder(string role) => role switch
	{
		"admin" => 0,
		"manager" => 1,
		"staff" => 2,
		_ => 3
	};

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
	{
		var text = await response.Content.ReadAsStringAsync();
		try
		{
			using var json = JsonDocument.Parse(text);
			if (json.RootElement.TryGetProperty("message", out var message))
				return message.GetString() ?? text;
			if (json.RootElement.ValueKind == JsonValueKind.String)
				return json.RootElement.GetString() ?? text;
		}
		catch { }
		return text.Trim('"');
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		Services.ApiClientProvider.ClearSession();
		App.ShowLogin();
	}
}

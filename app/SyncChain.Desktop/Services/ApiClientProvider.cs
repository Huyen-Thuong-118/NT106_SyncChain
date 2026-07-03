using System.Net.Http.Headers;

namespace SyncChain.Desktop.Services;

public static class ApiClientProvider
{
	public static string ApiBaseUrl { get; } = NormalizeBaseUrl(
		Environment.GetEnvironmentVariable("SYNCCHAIN_API_URL") ?? "http://localhost:5292/");

	public static string HubUrl => new Uri(new Uri(ApiBaseUrl), "hubs/chat").ToString();

	public static HttpClient Client { get; } = new()
	{
		BaseAddress = new Uri(ApiBaseUrl)
	};

	public static string? Token { get; private set; }
	public static string? Role { get; private set; }
	public static int? UserId { get; private set; }

	public static void SetSession(string token, string? role, int? userId = null)
	{
		Token = token;
		Role = role;
		UserId = userId;
		Client.DefaultRequestHeaders.Authorization =
			new AuthenticationHeaderValue("Bearer", token);
	}

	public static void ClearSession()
	{
		Token = null;
		Role = null;
		UserId = null;
		Client.DefaultRequestHeaders.Authorization = null;
	}

	// Kiểm tra backend còn sống + DB kết nối được (endpoint /health, ẩn danh).
	// Không bao giờ ném exception — trả false khi không kết nối được.
	public static async Task<bool> IsBackendHealthyAsync()
	{
		try
		{
			using var response = await Client.GetAsync("health");
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	private static string NormalizeBaseUrl(string value)
	{
		var trimmed = string.IsNullOrWhiteSpace(value) ? "http://localhost:5292/" : value.Trim();
		return trimmed.EndsWith("/", StringComparison.Ordinal) ? trimmed : trimmed + "/";
	}
}

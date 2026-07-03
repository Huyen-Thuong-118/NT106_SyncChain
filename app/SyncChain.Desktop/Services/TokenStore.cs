namespace SyncChain.Desktop.Services;

public static class TokenStore
{
    public static string? Token { get; private set; }
    public static int UserId { get; private set; }
    public static string? Role { get; private set; }
    public static string? HoTen { get; private set; }
    public static string? Email { get; private set; }

    public static void Save(string token, int userId, string role, string hoTen, string email)
    {
        Token = token;
        UserId = userId;
        Role = role;
        HoTen = hoTen;
        Email = email;
        ApplyToHttpClient();
    }

    public static void Clear()
    {
        Token = null;
        UserId = 0;
        Role = null;
        HoTen = null;
        Email = null;
        ApplyToHttpClient();
    }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    private static void ApplyToHttpClient()
    {
        var client = AppHttpClient.Instance;
        client.DefaultRequestHeaders.Authorization = Token is null
            ? null
            : new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
    }
}

// HttpClient singleton dùng chung toàn app
public static class AppHttpClient
{
    public static readonly HttpClient Instance = new()
    {
        BaseAddress = new Uri(ApiConfig.BaseUrl)
    };
}

public static class ApiConfig
{
    public const string BaseUrl = "http://localhost:5292/";
}

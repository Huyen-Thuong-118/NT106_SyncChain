using Microsoft.Extensions.Logging;

namespace SyncChain.Desktop;

public static class MauiProgram
{
	// ═══════════════════════════════════════════════════════
	//  BACKEND BASE URL — đổi thành địa chỉ server thật
	// ═══════════════════════════════════════════════════════
	public static string ApiBaseUrl => Services.ApiClientProvider.ApiBaseUrl;

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("SourceSerif4-Medium.ttf", "SourceSerif4");
				fonts.AddFont("SourceSerif4-SemiBold.ttf", "SourceSerif4");
				fonts.AddFont("Inter-Regular.ttf", "Inter");
				fonts.AddFont("Inter-SemiBold.ttf", "Inter");
				fonts.AddFont("Inter-Bold.ttf", "Inter");
				fonts.AddFont("MaterialSymbolsOutlined.ttf", "MaterialSymbols");
			});

		// Đăng ký HttpClient dùng chung cho toàn app
		builder.Services.AddSingleton(sp =>
		{
			return Services.ApiClientProvider.Client;
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

using Microsoft.Extensions.Logging;
using SyncChain.Desktop.Services;
using SyncChain.Desktop.Views.Pages;

namespace SyncChain.Desktop;

public static class MauiProgram
{
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

		// Dùng HttpClient singleton có token từ TokenStore
		builder.Services.AddSingleton(sp => AppHttpClient.Instance);

		// Đăng ký các trang cần DI
		builder.Services.AddTransient<CreateOrderPage>();
		builder.Services.AddTransient<ProfilePage>();
		builder.Services.AddTransient<AddressPage>();
		builder.Services.AddTransient<CartPage>();
		builder.Services.AddTransient<ForgotPasswordPage>();
		builder.Services.AddTransient<OrderDetailPage>();
		builder.Services.AddTransient<PaymentPage>();
		builder.Services.AddTransient<OrderTrackingPage>();
		builder.Services.AddTransient<NotificationPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

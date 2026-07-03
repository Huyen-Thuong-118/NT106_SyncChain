using Microsoft.Extensions.Logging;

#if WINDOWS
using Microsoft.Maui.Handlers;
using WinFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WinColor = Windows.UI.Color;
using WinSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WinThickness = Microsoft.UI.Xaml.Thickness;
#endif

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

#if WINDOWS
		PickerHandler.Mapper.AppendToMapping("SyncChainPickerBorder", (handler, _) =>
		{
			ApplyPickerChrome(handler.PlatformView);
		});

		DatePickerHandler.Mapper.AppendToMapping("SyncChainDatePickerBorder", (handler, _) =>
		{
			ApplyPickerChrome(handler.PlatformView);
		});

		EntryHandler.Mapper.AppendToMapping("SyncChainEntryBorder", (handler, _) =>
		{
			ApplyTextInputChrome(handler.PlatformView);
		});

		EditorHandler.Mapper.AppendToMapping("SyncChainEditorBorder", (handler, _) =>
		{
			ApplyTextInputChrome(handler.PlatformView);
		});
#endif

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

#if WINDOWS
	private static void ApplyPickerChrome(dynamic control)
	{
		var borderBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 128, 139, 150));
		var focusBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 33, 49, 69));
		var backgroundBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 255, 255, 255));
		var foregroundBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 11, 28, 48));

		control.BorderBrush = borderBrush;
		control.BorderThickness = new WinThickness(1.25);
		control.Background = backgroundBrush;
		control.Foreground = foregroundBrush;
		control.MinHeight = 44;
		control.Padding = new WinThickness(14, 0, 12, 0);

		if (control is WinFrameworkElement element)
		{
			element.Resources["ComboBoxBorderBrush"] = borderBrush;
			element.Resources["ComboBoxBorderBrushPointerOver"] = borderBrush;
			element.Resources["ComboBoxBorderBrushPressed"] = borderBrush;
			element.Resources["ComboBoxBorderBrushFocused"] = focusBrush;
			element.Resources["ComboBoxBackground"] = backgroundBrush;
			element.Resources["ComboBoxBackgroundPointerOver"] = backgroundBrush;
			element.Resources["ComboBoxBackgroundPressed"] = backgroundBrush;
			element.Resources["ComboBoxBackgroundFocused"] = backgroundBrush;
			element.Resources["ComboBoxForeground"] = foregroundBrush;
			element.Resources["ComboBoxForegroundPointerOver"] = foregroundBrush;
			element.Resources["ComboBoxDropDownGlyphForeground"] = foregroundBrush;
			element.Resources["ComboBoxDropDownGlyphForegroundPointerOver"] = foregroundBrush;
			element.Resources["ComboBoxDropDownGlyphForegroundPressed"] = foregroundBrush;
			element.Resources["ComboBoxBorderThemeThickness"] = new WinThickness(1.25);
		}
	}

	private static void ApplyTextInputChrome(dynamic control)
	{
		var borderBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 128, 139, 150));
		var focusBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 33, 49, 69));
		var backgroundBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 255, 255, 255));
		var foregroundBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 11, 28, 48));
		var placeholderBrush = new WinSolidColorBrush(WinColor.FromArgb(255, 67, 71, 75));

		control.BorderBrush = borderBrush;
		control.BorderThickness = new WinThickness(1.25);
		control.Background = backgroundBrush;
		control.Foreground = foregroundBrush;
		control.MinHeight = 44;
		control.Padding = new WinThickness(14, 0, 12, 0);

		if (control is WinFrameworkElement element)
		{
			element.Resources["TextControlBorderBrush"] = borderBrush;
			element.Resources["TextControlBorderBrushPointerOver"] = borderBrush;
			element.Resources["TextControlBorderBrushFocused"] = focusBrush;
			element.Resources["TextControlBackground"] = backgroundBrush;
			element.Resources["TextControlBackgroundPointerOver"] = backgroundBrush;
			element.Resources["TextControlBackgroundFocused"] = backgroundBrush;
			element.Resources["TextControlForeground"] = foregroundBrush;
			element.Resources["TextControlForegroundPointerOver"] = foregroundBrush;
			element.Resources["TextControlPlaceholderForeground"] = placeholderBrush;
			element.Resources["TextControlPlaceholderForegroundPointerOver"] = placeholderBrush;
			element.Resources["TextControlBorderThemeThickness"] = new WinThickness(1.25);
		}
	}
#endif
}

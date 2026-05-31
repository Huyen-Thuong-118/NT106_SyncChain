using System.Globalization;

namespace SyncChain.Desktop.Converters;

public sealed class BoolToColorConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var flag = value is bool isTrue && isTrue;
		var mode = parameter?.ToString();

		return mode switch
		{
			"SelectedPayment" => flag ? Color.FromArgb("#e0f2fe") : Colors.White,       // PrimaryContainer
			"ActiveThread" => flag ? Color.FromArgb("#e0f2fe") : Colors.White,          // PrimaryContainer
			"MessageBubble" => flag ? Color.FromArgb("#e0f2fe") : Colors.White,              // PrimaryContainer / White
			"MessageText" => flag ? Color.FromArgb("#0b1c30") : Color.FromArgb("#0b1c30"),  // OnSurface / OnSurface
			"SelectedRole" => flag ? Color.FromArgb("#e0f2fe") : Colors.White,          // PrimaryContainer
			_ => flag ? Color.FromArgb("#e0f2fe") : Colors.White                        // PrimaryContainer
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}

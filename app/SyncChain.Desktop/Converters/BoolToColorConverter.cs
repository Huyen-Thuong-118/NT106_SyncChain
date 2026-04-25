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
			"SelectedPayment" => flag ? Color.FromArgb("#CCFBF1") : Colors.White,
			"ActiveThread" => flag ? Color.FromArgb("#E0F2FE") : Colors.White,
			"MessageBubble" => flag ? Color.FromArgb("#0F766E") : Color.FromArgb("#E2E8F0"),
			"MessageText" => flag ? Colors.White : Color.FromArgb("#0F172A"),
			"SelectedRole" => flag ? Color.FromArgb("#DBEAFE") : Colors.White,
			_ => flag ? Color.FromArgb("#DBEAFE") : Colors.White
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}

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
			"MessageBubble" => flag ? Color.FromArgb("#dbeafe") : Colors.White,
			"MessageBubbleStroke" => flag ? Color.FromArgb("#93c5fd") : Color.FromArgb("#d8e3ee"),
			"MessageText" => Color.FromArgb("#0b1c30"),
			"MessageMeta" => flag ? Color.FromArgb("#1e3a5f") : Color.FromArgb("#475569"),
			"SelectedRole" => flag ? Color.FromArgb("#e0f2fe") : Colors.White,          // PrimaryContainer
			_ => flag ? Color.FromArgb("#e0f2fe") : Colors.White                        // PrimaryContainer
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}

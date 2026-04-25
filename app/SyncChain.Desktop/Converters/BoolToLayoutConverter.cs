using System.Globalization;

namespace SyncChain.Desktop.Converters;

public sealed class BoolToLayoutConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var flag = value is bool isTrue && isTrue;
		return flag ? LayoutOptions.End : LayoutOptions.Start;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}

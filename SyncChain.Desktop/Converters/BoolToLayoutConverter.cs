using System.Globalization;

namespace SyncChain.Desktop.Converters;

public sealed class BoolToLayoutConverter : IValueConverter
{
	// Đổi bool thành vị trí căn trái/phải cho bong bóng chat.
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var flag = value is bool isTrue && isTrue;
		return flag ? LayoutOptions.End : LayoutOptions.Start;
	}

	// Không hỗ trợ chuyển ngược từ layout về bool.
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}

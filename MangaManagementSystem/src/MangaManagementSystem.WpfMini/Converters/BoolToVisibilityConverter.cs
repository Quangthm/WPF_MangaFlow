using System.Globalization;
using System.Windows;

namespace MangaManagementSystem.WpfMini.Converters;

/// <summary>
/// Chuyển đổi bool thành Visibility.
/// Parameter = "false" hoặc "invert" để đảo ngược (true → Collapsed).
/// </summary>
public class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        bool invert = parameter is string s &&
                      (s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                       s.Equals("invert", StringComparison.OrdinalIgnoreCase));

        if (invert)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility == Visibility.Visible;
        return false;
    }
}

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MangaManagementSystem.WpfMini.Converters;

/// <summary>
/// Converts a URL string to an ImageSource for binding to Image.Source.
/// Returns null for empty/null strings so fallback/visibility can work.
/// </summary>
public class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

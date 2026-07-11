using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MangaManagementSystem.WpfMini.Converters;

/// <summary>
/// Chuyển đổi status code thành màu sắc tương ứng.
/// Dùng cho StatusBadge (Background) và StatusText (Foreground).
/// </summary>
public class StatusToBrushConverter : System.Windows.Data.IValueConverter
{
    /// <summary>
    /// Singleton instance để dùng với x:Static trong XAML.
    /// </summary>
    public static StatusToBrushConverter Instance { get; } = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as string ?? string.Empty;

        // Mặc định: xám (unknown status)
        var color = "#95a5a6";

        switch (status)
        {
            // Proposal statuses
            case "PROPOSAL_DRAFT":
            case "DRAFT":
                color = "#7f8c8d"; // Gray
                break;

            case "UNDER_EDITORIAL_REVIEW":
            case "UNDER_REVIEW":
                color = "#f39c12"; // Orange
                break;

            case "UNDER_BOARD_REVIEW":
                color = "#3498db"; // Blue
                break;

            case "REVISION_REQUESTED":
                color = "#e67e22"; // Dark orange
                break;

            case "APPROVED":
            case "SERIALIZED":
                color = "#27ae60"; // Green
                break;

            case "CANCELLED":
                color = "#e74c3c"; // Red
                break;

            case "WITHDRAWN":
                color = "#95a5a6"; // Gray
                break;
        }

        if (targetType == typeof(Brush))
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        }

        return color;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using System.Globalization;
using System.Windows;

namespace MangaManagementSystem.WpfMini.Converters;

/// <summary>
/// Chuyển đổi role code string thành Visibility.
/// Dùng để ẩn/hiện controls dựa trên role người dùng.
/// Parameter là role code cần so sánh, phân cách bởi dấu phẩy.
/// Ví dụ: Parameter="EDITOR,BOARD_CHIEF" sẽ Visible nếu role là Editor hoặc Board Chief.
/// </summary>
public class RoleToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string roleCode || parameter is not string allowedRoles)
            return Visibility.Collapsed;

        var roles = allowedRoles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return Array.Exists(roles, r => r.Equals(roleCode, StringComparison.OrdinalIgnoreCase))
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

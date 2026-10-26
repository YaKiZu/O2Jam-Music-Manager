using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace O2JamManager.Converters;

public static class BrushCache
{
    public static SolidColorBrush CreateFrozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}

public class StatusColorConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenText = BrushCache.CreateFrozen(Color.FromRgb(52, 211, 153));
    private static readonly SolidColorBrush RedText = BrushCache.CreateFrozen(Color.FromRgb(251, 113, 133));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool exists && exists)
            return GreenText;
        return RedText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StatusBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBg = BrushCache.CreateFrozen(Color.FromArgb(220, 6, 78, 59));
    private static readonly SolidColorBrush RedBg = BrushCache.CreateFrozen(Color.FromArgb(220, 76, 5, 25));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool exists && exists)
            return GreenBg;
        return RedBg;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StatusBorderConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBorder = BrushCache.CreateFrozen(Color.FromRgb(16, 185, 129));
    private static readonly SolidColorBrush RedBorder = BrushCache.CreateFrozen(Color.FromRgb(244, 63, 94));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool exists && exists)
            return GreenBorder;
        return RedBorder;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

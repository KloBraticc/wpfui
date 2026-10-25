using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Wpf.Ui.Converters;

public class ClipConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 3)
        {
            return null;
        }

        if (
            values[0] is not double width
            || values[1] is not double height
            || values[2] is not double percentage
        )
        {
            return null;
        }

        double clippedWidth = width * percentage;

        return new RectangleGeometry(new Rect(0, 0, clippedWidth, height));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace Snap.Nicole.UI.Xaml.Data;

internal sealed class NullableNumberBoxValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null || value == DependencyProperty.UnsetValue)
        {
            return double.NaN;
        }

        return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is not double number || double.IsNaN(number))
        {
            return null;
        }

        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            return ConvertToNullableInt32(number);
        }

        if (targetType == typeof(float) || targetType == typeof(float?))
        {
            return (float)number;
        }

        if (targetType == typeof(double) || targetType == typeof(double?))
        {
            return number;
        }

        return number;
    }

    private static int? ConvertToNullableInt32(double value)
    {
        if (value <= 0)
        {
            return null;
        }

        if (value >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}

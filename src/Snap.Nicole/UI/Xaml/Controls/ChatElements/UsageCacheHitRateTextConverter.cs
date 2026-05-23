using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

internal sealed class UsageCacheHitRateTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0:P0}", value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

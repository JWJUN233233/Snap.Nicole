using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

internal sealed class UsageCountVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

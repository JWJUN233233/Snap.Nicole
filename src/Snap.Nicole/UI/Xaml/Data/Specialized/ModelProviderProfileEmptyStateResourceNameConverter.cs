using Microsoft.UI.Xaml.Data;
using Snap.Nicole.Resources;
using System;

namespace Snap.Nicole.UI.Xaml.Data.Specialized;

internal sealed class ModelProviderProfileEmptyStateResourceNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is int count && count > 0
            ? SRName.UIXamlPagesSettingsPageDescriptionSelectModelProviderProfileToEdit
            : SRName.UIXamlPagesSettingsPageDescriptionAddModelProviderProfileToEdit;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

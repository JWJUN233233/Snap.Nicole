using Microsoft.UI.Xaml.Data;
using Snap.Nicole.Resources;
using System;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

internal sealed class ReasoningContentTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isCompleted = value is bool boolValue && boolValue;
        string resourceName = isCompleted
            ? nameof(SRName.UIXamlControlsChatMessageViewLabelReasoningCompleted)
            : nameof(SRName.UIXamlControlsChatMessageViewLabelReasoningInProgress);

        return StringResourceProxy.Default[resourceName];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

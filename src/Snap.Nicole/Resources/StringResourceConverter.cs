using Microsoft.UI.Xaml.Data;

namespace Snap.Nicole.Resources;

internal sealed class StringResourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        object? name = parameter is IValueConverter converter
            ? converter.Convert(value, typeof(SRName), null!, language)
            : parameter ?? value;

        return name switch
        {
            SRName resourceName => StringResourceProxy.Default[string.Intern($"{resourceName}")],
            string resourceName => StringResourceProxy.Default[resourceName],
            _ => string.Empty,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
